using DnsServerCore.ApplicationCommon;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TechnitiumLibrary.Net.Dns;
using TechnitiumLibrary.Net.Dns.ResourceRecords;
using Elastic.Clients.Elasticsearch;
using System.Linq;
using Elastic.Transport;

namespace QueryLogsElastic
{
    public class App : IDnsApplication, IDnsQueryLogger
    {
        #region variables

        IDnsServer _dnsServer;

        private bool enableLogging;
        private string hosts;
        private string index;
        private string username;
        private string password;
        private string fingerprint;

        private ElasticsearchClientSettings clientSettings;
        private ElasticsearchClient client;

        #endregion

        #region properties
        public string Description
        {
            get { return "Logs all DNS requests to an ElasticSearch database."; }
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            enableLogging = false;
        }
        #endregion

        public Task InitializeAsync(IDnsServer dnsServer, string config)
        {
            _dnsServer = dnsServer;
            dynamic jsonConfig = JsonConvert.DeserializeObject(config);

            enableLogging = jsonConfig.enableLogging.Value;
            hosts = jsonConfig.hosts.Value;
            index = jsonConfig.index.Value;
            username = jsonConfig.username.Value;
            password = jsonConfig.password.Value;
            //fingerprint = jsonConfig.fingerprint.Value;

            try
            {
                var hostUri = new Uri(hosts);
                clientSettings = new ElasticsearchClientSettings(hostUri);
                clientSettings.Authentication(new BasicAuthentication(username, password));
                clientSettings.DefaultIndex(index);
                clientSettings.EnableDebugMode();
                clientSettings.PrettyJson();

                client = new ElasticsearchClient(clientSettings);
            }
            catch(Exception ex)
            {
                _dnsServer.WriteLog(ex);
                throw new Exception(string.Format("Error setting up Elastic Client: {0}", ex.Message), ex);
            }

            return Task.CompletedTask;
        }


        //public async Task<IndexResponse> InsertLogAsync(DateTime timestamp, DnsDatagram request, IPEndPoint remoteEP, DnsTransportProtocol protocol, DnsDatagram response)
        //{
        //    if (!enableLogging)
        //    {
        //        return null; // new IndexResponse();
        //    }
        //    IndexResponse indexResponse;
        //    var entry = new LogEntry(timestamp, request, remoteEP, protocol, response);
        //    indexResponse = await client.IndexAsync(entry);
        //    //return Task.FromResult(indexResponse);
        //    return indexResponse;
        //}
        public Task InsertLogAsync(DateTime timestamp, DnsDatagram request, IPEndPoint remoteEP, DnsTransportProtocol protocol, DnsDatagram response)
        {
            if (!enableLogging)
            {
                return Task.CompletedTask; // new IndexResponse();
            }
            //IndexResponse indexResponse;
            var entry = new LogEntry(timestamp, request, remoteEP, protocol, response);
            var indexResponse = client.Index(entry);

            //return Task.FromResult(indexResponse);
            //return indexResponse;
            return Task.CompletedTask;
        }

        public Task<DnsLogPage> QueryLogsAsync(long pageNumber, int entriesPerPage, bool descendingOrder, DateTime? start, DateTime? end, IPAddress clientIpAddress, DnsTransportProtocol? protocol, DnsServerResponseType? responseType, DnsResponseCode? rcode, string qname, DnsResourceRecordType? qtype, DnsClass? qclass)
        {
            var startIndex = 0;
            if (pageNumber > 1)
            {
                startIndex = entriesPerPage * (int)(pageNumber - 1);
            }

            var searchRequest = new SearchRequest()
            {
                Size = entriesPerPage,
                From = startIndex,
            };

            //var searchResponse = client.SearchAsync<LogEntry>(searchRequest);
            var search = client.Search<LogEntry>(searchRequest);
            var results = search.Documents;
            List<DnsLogEntry> entries = new List<DnsLogEntry>(entriesPerPage);
            // loop through documents, create DnsLogEntry object
            // expensive data conversions ahead
            throw new NotImplementedException();
        }

        class LogEntry
        {
            #region variables

            public readonly DateTime Timestamp;
            public readonly DnsDatagram Request;
            public readonly IPEndPoint RemoteEP;
            public readonly DnsTransportProtocol Protocol;
            public readonly DnsDatagram Response;

            #endregion

            #region constructor

            public LogEntry(DateTime timestamp, DnsDatagram request, IPEndPoint remoteEP, DnsTransportProtocol protocol, DnsDatagram response)
            {
                Timestamp = timestamp;
                Request = request;
                RemoteEP = remoteEP;
                Protocol = protocol;
                Response = response;
            }

            #endregion
        }
    }
}