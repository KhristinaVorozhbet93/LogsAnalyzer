﻿using IPAddressLogAnalyzer.Domain.Entities;
using IPAddressLogAnalyzer.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
namespace IPAddressLogAnalyzer.FilterService
{
    public class LogFilterService : ILogFilterService
    {
        private readonly DateTime _timeStart;
        private readonly DateTime _timeEnd;
        private readonly string? _addressStart;
        private readonly string? _addressMask;
        public LogFilterService(IOptions<IPConfiguration> options)
        {
            _timeStart = options.Value.TimeStart;
            _timeEnd = options.Value.TimeEnd;
            _addressStart = options.Value.AddressStart;
            _addressMask = options.Value.AddressMask;
        }

        public List<LogRecord> GetIPAddressesWithConfigurations(List<LogRecord> logs)
        {
            logs.Sort();
            var timeAddresses = GetIPAddressesInTimeInterval(logs, _timeStart, _timeEnd);

            var countTimeRequestLogs = GetIPAddressesWithCountTimeRequests(timeAddresses);

            if (!string.IsNullOrEmpty(_addressStart) && !string.IsNullOrEmpty(_addressMask))
            {
                var filtredLogs = GetRangeIPAddresses
                    (countTimeRequestLogs, IPAddress.Parse(_addressStart), IPAddress.Parse(_addressMask));
                return filtredLogs;
            }
            return countTimeRequestLogs;
        }

        public List<LogRecord> GetRangeIPAddresses(List<LogRecord> logs, IPAddress addressStart, IPAddress addressMask)
        {
            List<LogRecord> filteredLogs = new List<LogRecord>();
            foreach (var log in logs)
            {
                if (IsIPAddressInRange
                    (log.ClientIpAddress, addressStart, addressMask))
                {
                    filteredLogs.Add(log);
                }
            }
            return filteredLogs;
        }
        public List<LogRecord> GetIPAddressesInTimeInterval(List<LogRecord> logs, DateTime timeStart, DateTime timeEnd)
        {
            return logs.Where(ip =>
                    ip.TimeRequest <= timeEnd &&
                    ip.TimeRequest >= timeStart)
                    .ToList();
        }
        public List<LogRecord> GetIPAddressesWithCountTimeRequests(List<LogRecord> logs)
        {
            return logs
                .GroupBy(ip => ip.ClientIpAddress) 
                .Select(group => new LogRecord(
                    group.Key,
                    group.First().TimeRequest, 
                    group.Sum(log => log.RequestCount), 
                    group.First().Resource, 
                    group.First().Path, 
                    group.First().Method, 
                    group.First().Response
                ))
                .ToList();
        }

        private bool IsIPAddressInRange(IPAddress ipAddress, IPAddress addressStart, IPAddress addressMask)
        {
            byte[] ipBytes = ipAddress.GetAddressBytes();
            byte[] startBytes = addressStart.GetAddressBytes();
            byte[] maskBytes = addressMask.GetAddressBytes();

            if (ipBytes.Length != startBytes.Length || startBytes.Length != maskBytes.Length)
            {
                return false;
            }

            for (int i = 0; i < ipBytes.Length; i++)
            {
                if ((ipBytes[i] & maskBytes[i]) < (startBytes[i] & maskBytes[i]))
                {
                    return false;
                }

                if ((ipBytes[i] & maskBytes[i]) > (startBytes[i] & maskBytes[i]) + (255 - maskBytes[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}