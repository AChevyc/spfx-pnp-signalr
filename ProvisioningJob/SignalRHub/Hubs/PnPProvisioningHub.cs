﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NLog;

namespace SignalRHub.Hubs
{
    [Authorize]
    public class PnPProvisioningHub : Hub
    {
        private readonly ILogger _logger;

        public PnPProvisioningHub()
        {
            _logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        }

        // notifies all connected clients about progress change
        public Task Notify(object data)
        {
            return Clients.All.SendAsync("notify", data);
        }

        // notifies all connected clients that the provisioning is completed
        public Task Completed()
        {
            return Clients.All.SendAsync("completed");
        }

        // notifies specific client about the current state of the provisioning process
        public void InitialState(string webUrl)
        {
            try
            {
                var rowKey = webUrl.Split('/').Last();

                var tableManager = new TableManager(Consts.TableName, Settings.StorageConnection);
                var state = tableManager.GetByKey<ProvisioningState>(rowKey);

                if (state == null)
                {
                    Clients.Client(Context.ConnectionId).SendAsync("initial-state", new
                    {
                        Total = -1
                    });
                    return;
                }

                Clients.Client(Context.ConnectionId).SendAsync("initial-state", new
                {
                    state.Message,
                    state.Progress,
                    state.Total
                });
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }
    }
}
