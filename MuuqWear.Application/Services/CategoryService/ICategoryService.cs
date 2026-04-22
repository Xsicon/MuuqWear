using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuuqWear.Application.Services.CategoryService;
public interface ICategoryService
{
    Task<Response<List<CategoryModel>>> GetAll();
    Task<Response<CategoryModel>> Add(AddCategoryModel request);
}
