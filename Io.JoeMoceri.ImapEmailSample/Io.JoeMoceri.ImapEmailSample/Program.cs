namespace Io.JoeMoceri.ImapEmailSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = "";
            var username = "";
            var password = "";
            var app = new App(host, username, password);

            app.Run();
        }
    }
}
