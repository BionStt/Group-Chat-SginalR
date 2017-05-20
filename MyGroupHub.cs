using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalRChat
{
    /* 
     * Now let us start signalR class for group chat. Some peoples thinks that, 
     * “Clients.All.receiveMessage(msgFrom, msg, "");” is useful to use in group chat. 
     * But it is not correct because this will broadcast message to all connected clients 
     * and not to any particular group. To maintain different groups, use of “Groups” method of signalR is 
     * necessary.
     **/

    // [HubName("GroupChatHub")] is the my custom hub class name used for group chat.
    [HubName("GroupChatHub")]
    public class MyGroupHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }

        // Following method is used to broadcast messages to a particular group.
        public void BroadCastMessage(String msgFrom, String msg, String GroupName)
        {
            var id = Context.ConnectionId;
            string[] Exceptional = new string[0];
            Clients.Group(GroupName,Exceptional).receiveMessage(msgFrom, msg, "");
            //Clients.All.receiveMessage(msgFrom, msg, "");
            /*string[] Exceptional = new string[1];
            Exceptional[0] = id;       
            Clients.AllExcept(Exceptional).receiveMessage(msgFrom, msg);*/
        }
        /* 
         * Following custom method is written to connect current user to a particular group. 
         * The groupname was entered by user only.
         * **/
        [HubMethodName("groupconnect")]
        public void Get_Connect(String username, String userid, String connectionid, String GroupName)
        {
            string count = "NA";
            string msg = "Welcome to group "+GroupName;
            string list = "";
           
            var id = Context.ConnectionId;
            //this will add the connected user to particular group
            Groups.Add(id, GroupName);

            string[] Exceptional = new string[1];
            Exceptional[0] = id;

            Clients.Caller.receiveMessage("Group Chat Hub", msg, list);
            Clients.OthersInGroup(GroupName).receiveMessage("NewConnection", GroupName+" "+username + " " + id, count);
            //Clients.AllExcept(Exceptional).receiveMessage("NewConnection", username + " " + id, count);
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            //string username = Context.QueryString["username"].ToString();
            string clientId = Context.ConnectionId;
            string data = clientId;
            string count = "NA";
            Clients.Caller.receiveMessage("ChatHub", data, count);
            return base.OnConnected();
        }

        public override System.Threading.Tasks.Task OnReconnected()
        {
            return base.OnReconnected();
        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            string count = "NA";
            string msg = "";

            string clientId = Context.ConnectionId;
            string[] Exceptional = new string[1];
            Exceptional[0] = clientId;
            Clients.AllExcept(Exceptional).receiveMessage("NewConnection", clientId + " leave", count);

            return base.OnDisconnected(stopCalled);
        }
    }
}