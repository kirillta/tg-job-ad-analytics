using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgJobAdAnalytics.Data;

namespace TgJobAdAnalytics.Services.Messages;

public sealed class AdProcessor
{
    public AdProcessor(ApplicationDbContext dbContext, ParallelOptions parallelOptions)
    {
        _dbContext = dbContext;
        _parallelOptions = parallelOptions;
    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ParallelOptions _parallelOptions;
}
