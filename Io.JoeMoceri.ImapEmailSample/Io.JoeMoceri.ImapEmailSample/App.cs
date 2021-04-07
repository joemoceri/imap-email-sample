namespace Io.JoeMoceri.ImapEmailSample
{
    using MailKit.Search;
    using System;

    public class App
    {
        private IImap imap;
        private readonly string host;
        private readonly string username;
        private readonly string password;

        public App(string host, string username, string password)
        {
            this.host = host;
            this.username = username;
            this.password = password;
        }

        public void Run()
        {
            imap = new Imap(host, username, password);

            imap.ActivateImapListener(CountChangedCallback);

            imap.Dispose();

            // fired when the count on the inbox changes (message arrived, deleted, archived, etc)
            void CountChangedCallback(object sender, EventArgs e)
            {
                CheckMailbox();
            }
        }

        private void CheckMailbox()
        {
            try
            {
                // Once the count has changed, get all messages in a separate imap client
                var searchQuery = SearchQuery.All;

                var newImapClient = new Imap(host, username, password);

                var messages = newImapClient.GetMessages(searchQuery);

                foreach (var message in messages)
                {
                    // whatever you want to do
                }

                newImapClient.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); // keep listening for new mailboxes
            }
        }
    }
}
