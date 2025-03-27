using System;
using System.Collections.Generic;

namespace Backend.Models
{
    public class QueryParameter
    {
        public required string Name { get; set; }
        public string? Value { get; set; }
    }

    public class UrlAnalysisResult
    {
        public required string OriginalUrl { get; set; }
        public required string Scheme { get; set; }
        public required string Host { get; set; }
        public int? Port { get; set; }
        public required string Path { get; set; }
        public required string Query { get; set; }
        public required List<QueryParameter> QueryParameters { get; set; }
        public required string Fragment { get; set; }
        public required bool IsValid { get; set; }
        public string? ValidationError { get; set; }
        public required bool IsAvailable { get; set; }
        public required string[] DnsRecords { get; set; }
        public required string AddressType { get; set; }
        public required DateTime Timestamp { get; set; }
        public string? UserInfo { get; set; }
        public string? Authority { get; set; }
        public string? AbsoluteUri { get; set; }
        public string? LocalPath { get; set; }
        public string? QueryString { get; set; }
        public string? PathAndQuery { get; set; }
    }
} 