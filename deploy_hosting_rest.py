"""Deploy hosting-wwwroot through the Hosting REST API. Does not print tokens."""
from __future__ import annotations

import gzip
import hashlib
import json
import os
import subprocess
import ssl
import sys
import urllib.error
import urllib.request
from pathlib import Path

SITE = "muuqwear-6e2e0"
ROOT = Path(__file__).resolve().parent / "hosting-wwwroot"
API = "https://firebasehosting.googleapis.com/v1beta1"


def token() -> str:
    value = os.environ.get("HOSTING_ACCESS_TOKEN", "").strip()
    if value:
        return value
    raise SystemExit("HOSTING_ACCESS_TOKEN is missing")


def request(url: str, access: str, data: dict | bytes | None = None, method: str = "GET") -> dict:
    headers = {
        "authorization": f"Bearer {access}",
        "x-goog-user-project": SITE,
    }
    body = None
    if isinstance(data, dict):
        headers["content-type"] = "application/json"
        body = json.dumps(data).encode()
    elif isinstance(data, bytes):
        headers["content-type"] = "application/octet-stream"
        body = data
    req = urllib.request.Request(url, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, context=ssl.create_default_context(), timeout=120) as response:
            raw = response.read().decode()
            return json.loads(raw) if raw else {}
    except urllib.error.HTTPError as error:
        raise SystemExit(f"hosting HTTP {error.code} {error.read()[:300]!r}") from error


def gzip_hash(path: Path) -> tuple[str, bytes]:
    compressed = gzip.compress(path.read_bytes(), compresslevel=9)
    return hashlib.sha256(compressed).hexdigest(), compressed


def main() -> int:
    access = token()
    files: dict[str, str] = {}
    blobs: dict[str, bytes] = {}
    for path in ROOT.rglob("*"):
        if not path.is_file():
            continue
        rel = "/" + path.relative_to(ROOT).as_posix()
        digest, compressed = gzip_hash(path)
        files[rel] = digest
        blobs[digest] = compressed

    version = request(
        f"{API}/sites/{SITE}/versions",
        access,
        {
            "config": {
                "headers": [
                    {
                        "glob": "/_framework/**",
                        "headers": {"Cache-Control": "public, max-age=31536000, immutable"},
                    }
                ],
                "rewrites": [
                    {
                        "glob": "**",
                        "run": {"serviceId": "muuqwear-web", "region": "europe-west2"},
                    }
                ],
            }
        },
        method="POST",
    )
    name = version["name"]
    keys = list(files)
    upload_url = ""
    required: list[str] = []
    for start in range(0, len(keys), 900):
        chunk = {key: files[key] for key in keys[start : start + 900]}
        populated = request(f"{API}/{name}:populateFiles", access, {"files": chunk}, method="POST")
        upload_url = populated.get("uploadUrl") or upload_url
        required.extend(populated.get("uploadRequiredHashes") or [])

    for digest in required:
        request(f"{upload_url}/{digest}", access, blobs[digest], method="POST")

    request(f"{API}/{name}?updateMask=status", access, {"status": "FINALIZED"}, method="PATCH")
    request(f"{API}/sites/{SITE}/releases?versionName={name}", access, {}, method="POST")
    print(f"hosting_ok files={len(files)} uploaded={len(required)} version={name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
