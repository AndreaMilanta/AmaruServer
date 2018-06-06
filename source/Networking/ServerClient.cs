using System.Net.Sockets;

using ClientServer.Communication;
using ClientServer.Messages;
using AmaruCommon.Messages;
using AmaruCommon.Constants;
using AmaruCommon.Exceptions;

namespace AmaruServer.Networking
{
    public class ServerClient : ClientTCP
    {

        public ServerClient(Socket soc, int bufferSize, string log) : base(soc, bufferSize, log)
        {
        }

        protected override void HandleNewMessage(Message mex)
        {
            // Check that mex is of valid type
            if (!(mex is AmaruMessage))
            {
                LogException(new InvalidMessageException().ToString());
                _failCount++;
                _consecutiveFailCount++;
                if (_consecutiveFailCount >= NetworkConstants.MaxConsecutiveFailures)
                {
                    LogException("Too many consecutive failures");
                    this.Close();
                }
                if (_failCount >= NetworkConstants.MaxFailures)
                {
                    LogException("Too many failures");
                    this.Close();
                }
                return;
            }
            else
                _consecutiveFailCount = 0;

            // Logical switch on mex type
            if (mex is LoginMessage)
            {
                ConnectionManager.Instance.NewLogin(this, (LoginMessage)mex);
            }
        }
    }
}
