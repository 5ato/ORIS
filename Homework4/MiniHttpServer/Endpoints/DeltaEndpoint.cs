using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Services;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    public class DeltaEndpoint
    {
        // get
        [HttpGet]
        public string LoginPage()
        {
            return "index.html";
        }

        // post
        [HttpPost]
        public string Login(string email, string password)
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
            return "index.html";
        }
    }
}