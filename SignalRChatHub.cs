using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;


namespace SignalRChat
{
    [HubName("signalRChatHub")]
    public class SignalRChatHub : Hub
    {
        #region---Data Members---
        static List<UserDetail> ConnectedUsers = new List<UserDetail>();
        static List<MessageDetail> CurrentMessage = new List<MessageDetail>();
        #endregion
      
        SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["CHCON"].ConnectionString);

        public void BroadCastMessage(String msgFrom, String msg)
        {                      
            var id = Context.ConnectionId;
            Clients.All.receiveMessage(msgFrom, msg, "");            
            /*string[] Exceptional = new string[1];
            Exceptional[0] = id;       
            Clients.AllExcept(Exceptional).receiveMessage(msgFrom, msg);*/           
        }

        [HubMethodName("hubconnect")]
        public void Get_Connect(String username,String userid,String connectionid)
        {
            string count = "";
            string msg = "";
            string list = "";
            try
            {
                count = GetCount().ToString();
                msg = updaterec(username, userid, connectionid);
                list = GetUsers(username);
            }
            catch (Exception d)
            {
                msg = "DB Error "+d.Message;
            }
            var id = Context.ConnectionId;
            
            string[] Exceptional = new string[1];
            Exceptional[0] = id;
            Clients.Caller.receiveMessage("RU", msg, list);
            Clients.AllExcept(Exceptional).receiveMessage("NewConnection", username+" "+id,count);            
        }

        //[HubMethodName("privatemessage")]
        public void Send_PrivateMessage(String msgFrom, String msg, String touserid)
        {
            var id = Context.ConnectionId;
            Clients.Caller.receiveMessage(msgFrom, msg,touserid);
            Clients.Client(touserid).receiveMessage(msgFrom, msg,id);
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            //string username = Context.QueryString["username"].ToString();
            string clientId = Context.ConnectionId;
            string data =clientId;
            string count = "";
            try
            {
                count= GetCount().ToString();
            }
            catch (Exception d)
            {
                count = d.Message;
            }            
            Clients.Caller.receiveMessage("ChatHub", data, count);
            return base.OnConnected();
        }

        public override System.Threading.Tasks.Task OnReconnected()
        {         
            return base.OnReconnected();
        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            string count = "";
            string msg = "";
           
            string clientId = Context.ConnectionId;
            DeleteRecord(clientId);

            try
            {
                count = GetCount().ToString();
            }
            catch (Exception d)
            {
                msg = "DB Error " + d.Message;
            }
            string[] Exceptional = new string[1];
            Exceptional[0] = clientId;
            Clients.AllExcept(Exceptional).receiveMessage("NewConnection", clientId+" leave", count);

            return base.OnDisconnected(stopCalled);
        }

        public string updaterec(string username,string userid, string connectionid)
        {
            try
            {
                SqlCommand save = new SqlCommand("insert into [ChatUsers] values('" + username + "','" + userid + "','" + connectionid + "')", sqlcon);
                sqlcon.Open();
                int rs = save.ExecuteNonQuery();
                sqlcon.Close();
                return "saved";
            }
            catch (Exception d)
            {
                sqlcon.Close();
                return d.Message;
            }            
        }

        public int GetCount()
        {
            int count = 0;

            try
            {
                SqlCommand getCount = new SqlCommand("select COUNT([UserName]) as TotalCount from [ChatUsers]", sqlcon);
                sqlcon.Open();
                count = int.Parse(getCount.ExecuteScalar().ToString());
            }
            catch (Exception)
            {
            }
            sqlcon.Close();
            return count;
        }

        public bool DeleteRecord(string connectionid)
        {
            bool result = false;

            try
            {
                SqlCommand deleterec = new SqlCommand("delete from [ChatUsers] where ([ConnectionID]='" + connectionid + "')", sqlcon);
                sqlcon.Open();
                deleterec.ExecuteNonQuery();
                result = true;
            }
            catch (Exception)
            {   
            }
            sqlcon.Close();
            return result;
        }

        public string GetUsers(string username)
        {
            string list = "";

            try
            {
                int count = GetCount();
                SqlCommand listrec = new SqlCommand("select [UserName],[ConnectionID] from [ChatUSers] where ([UserName]<>'" + username + "')", sqlcon);
                sqlcon.Open();
                SqlDataReader reader = listrec.ExecuteReader();
                reader.Read();

                for (int i = 0; i < (count-1); i++)
                {
                    list += reader.GetValue(0).ToString() + " ( " + reader.GetValue(1).ToString() + " )#";
                    reader.Read();
                }
            }
            catch (Exception)
            {
            }
            sqlcon.Close();
            return list;
        }

        public void Create_Group(string GroupName)
        {
            
        }

        private string GetClientId()
        {
            string clientId = "";
            if (Context.QueryString["clientId"] != null)
            {
                // clientId passed from application 
                clientId = this.Context.QueryString["clientId"];
            }

            if (string.IsNullOrEmpty(clientId.Trim()))
            {
                clientId = Context.ConnectionId;
            }

            return clientId;
        }
    }
}