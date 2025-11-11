using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelHub.Domain.Interfaces.Services;

public interface ICurrencyConversionService
{
    Task<decimal> GetExchangeRate(string from, string to);
}
