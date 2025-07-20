using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Services.Salaries;

namespace TgJobAdAnalytics.Services.Messages;

public sealed class AdProcessor
{
    public AdProcessor(ApplicationDbContext dbContext, ParallelOptions parallelOptions, SalaryServiceFactory salaryServiceFactory)
    {
        _dbContext = dbContext;
        _parallelOptions = parallelOptions;
        _salaryServiceFactory = salaryServiceFactory;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ParallelOptions _parallelOptions;
    private readonly SalaryServiceFactory _salaryServiceFactory;
}
