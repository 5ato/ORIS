using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Services;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    public class AuthEndpoint : EndpointBase
    {
        // get
        [HttpGet("login")]
        public IHttpResult LoginPage()
        {
            Context.Request.Cookies.Clear();

            return Page("index.html", new object());
        }

        // post
        [HttpPost]
        public IHttpResult Login(string email, string password)
        {
            EmailService.SendEmail(email, "Авторизация", $@"
                                    <html>
                                    <body>
                                        <h2>Уведомление об авторизации</h2>
                                        <p>Вы успешно авторизовались в системе.</p>
                                        
                                        <p><strong>Ваш логин:</strong> {email}</p>
                                        <p><strong>Ваш пароль:</strong> {password}</p>
                                        
                                        <p>Сохраните эти данные для будущих входов в систему.</p>
                                    </body>
                                    </html>",
                                    "public/MiniHttpServer.zip");
            return Page("index.html", new object());
        }
    }
}