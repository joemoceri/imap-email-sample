namespace Io.JoeMoceri.ImapEmailSample
{
    using MailKit;
    using MailKit.Net.Imap;
    using MailKit.Search;
    using MimeKit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IImap : IDisposable
    {
        void ActivateImapListener(EventHandler<EventArgs> countChangedCallback);
        IEnumerable<MimeMessage> GetMessages(SearchQuery query);
        void DeleteMessages(params UniqueId[] uids);
    }

    public class Imap : IImap
    {
        private ImapClient imapClient;
        private readonly string Host;
        private readonly string Username;
        private readonly string Password;

        public Imap(string host, string username, string password)
        {
            Host = host;
            Username = username;
            Password = password;
        }

        public void ActivateImapListener(EventHandler<EventArgs> countChangedCallback)
        {
            using (var client = GetImapClient()) // this is the main client that runs
            {
                client.Inbox.Open(FolderAccess.ReadOnly);

                client.Inbox.CountChanged += countChangedCallback;

                using (var done = new CancellationTokenSource()) // keep listening indefinitely
                {
                    var task = client.IdleAsync(done.Token);
                    Task.Delay(new TimeSpan(0, 15, 0)).Wait(); // After 15 minutes, shut down the listener and let it start up again
                    done.Cancel();
                    task.Wait();
                }

                client.Disconnect(true);
            }
        }

        public void DeleteMessages(params UniqueId[] uids)
        {
            if (uids.Length == 0)
            {
                return;
            }

            using (var client = GetImapClient())
            {
                var folder = client.Inbox;

                folder.Open(FolderAccess.ReadWrite);

                foreach (var uid in uids)
                {
                    client.Inbox.AddFlags(uid, MessageFlags.Deleted, true); // this will be deleted after
                }

                // delete all emails that don't match the pattern specified
                client.Inbox.Expunge();
            }
        }

        public IEnumerable<MimeMessage> GetMessages(SearchQuery query)
        {
            using (var client = GetImapClient())
            {
                var folder = client.Inbox;

                folder.Open(FolderAccess.ReadWrite);

                var messages = new List<MimeMessage>();

                // ignore messages marked for deletion
                foreach (var uid in folder.Search(SearchQuery.And(query, SearchQuery.NotDeleted)))
                {
                    try
                    {
                        var message = folder.GetMessage(uid);

                        messages.Add(message);
                        var addresses = new List<MailboxAddress>();
                        foreach (var to in message.To)
                        {
                            var result = to as MailboxAddress;
                            if (result == null)
                            {
                                continue;
                            }
                            addresses.Add(result);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex); // keep trying each message
                    }
                }

                return messages;
            }
        }

        private ImapClient GetImapClient()
        {
            // check if it's connected or not initialized
            if (imapClient != null)
            {
                return imapClient;
            }

            // otherwise create a new one
            imapClient = new ImapClient();

            var credentials = new NetworkCredential(Username, Password);
            var uri = new Uri($"imaps://{Host}");

            imapClient.Connect(uri);

            // Remove the XOAUTH2 authentication mechanism since we don't have an OAuth2 token.
            imapClient.AuthenticationMechanisms.Remove("XOAUTH2");

            imapClient.Authenticate(credentials);

            return imapClient;
        }

        public void Dispose()
        {
            imapClient?.Dispose();
            imapClient = null;
        }
    }
}
