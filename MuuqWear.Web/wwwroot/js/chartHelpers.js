// =============================================
// CHART.JS HELPER FUNCTIONS
// =============================================

window.chartHelpers = {
    // Store chart instance for cleanup
    chartInstance: null,

    // Render performance chart
    renderPerformanceChart: function (chartData) {
        const ctx = document.getElementById('performanceChart');

        if (!ctx) {
            console.error('Canvas element not found');
            return false;
        }

        // Destroy existing chart if it exists
        if (this.chartInstance) {
            this.chartInstance.destroy();
        }

        // Extract data from the DTO
        const days = chartData.dailyStats.map(d => d.day);
        const clicks = chartData.dailyStats.map(d => d.clicks);
        const conversions = chartData.dailyStats.map(d => d.conversions);

        // Create the chart
        this.chartInstance = new Chart(ctx, {
            type: 'line',
            data: {
                labels: days,
                datasets: [
                    {
                        label: 'Clicks',
                        data: clicks,
                        borderColor: '#1E2A47',
                        backgroundColor: 'transparent',
                        borderWidth: 2,
                        tension: 0.4,  // Smooth curves
                        pointRadius: 0, // Hide points
                        pointHoverRadius: 5 // Show on hover
                    },
                    {
                        label: 'Conversions',
                        data: conversions,
                        borderColor: '#9BA8BE',
                        backgroundColor: 'transparent',
                        borderWidth: 2,
                        tension: 0.4,
                        pointRadius: 0,
                        pointHoverRadius: 5
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                aspectRatio: 4.5, // Width:Height ratio (adjust as needed)
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        display: true,
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            padding: 15,
                            color: '#4A5C7A',
                            font: {
                                size: 11
                            }
                        }
                    },
                    tooltip: {
                        backgroundColor: '#1E2A47',
                        titleColor: '#A9B7CC',
                        bodyColor: '#FFFFFF',
                        padding: 12,
                        displayColors: true,
                        callbacks: {
                            label: function (context) {
                                return context.dataset.label + ': ' + context.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#4A5C7A',
                            font: {
                                size: 11
                            }
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: '#E5E9F0',
                            lineWidth: 1
                        },
                        ticks: {
                            color: '#4A5C7A',
                            font: {
                                size: 11
                            },
                            stepSize: 25
                        }
                    }
                }
            }
        });

        return true;
    },

    // Cleanup function
    destroyChart: function () {
        if (this.chartInstance) {
            this.chartInstance.destroy();
            this.chartInstance = null;
        }
    }
};