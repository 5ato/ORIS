using System.Formats.Asn1;
using MiniTemplateEngine;

namespace MiniTemplateEngineUnitTest;


[TestClass]
public sealed class HtmlTemplateRendererTests
{
    [DataTestMethod]
    [DataRow("<h1>Привет ${Name}</h1><p>Привет ${Name}</p>", "<h1>Привет Aboba</h1><p>Привет Aboba</p>")]
    [DataRow("<h1>Привет ${Name}</h1><p>Привет ${Email}</p>", "<h1>Привет Aboba</h1><p>Привет test@test.ru</p>")]
    [DataRow("<h1>Привет ${Name}</h1><p> группа: ${Group.Name}</p>", "<h1>Привет Aboba</h1><p> группа: 11-409</p>")]
    [DataRow("<h1>Привет $ ${Name}</h1><p> группа: ${Group.Name}$</p>", "<h1>Привет $ Aboba</h1><p> группа: 11-409$</p>")]
    public void RenderFromString_PutVariableInString_ReturnCorrectString(string templateHtml, string expected)
    {
        // Arrange
        var testee = new HtmlTemplateRenderer();
        var model = new
        {
            Name = "Aboba",
            Email = "test@test.ru",
            Group = new
            {
                Id = 1,
                Name = "11-409",
            },
        };

        // Act
        var result = testee.RenderFromString(templateHtml, model);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    // Проверка на блок if
    [DataRow("$if(IsTrue) <p>User is active</p>$endif", " <p>User is active</p>")]
    // Проверка на блок if и есть else
    [DataRow("$if(IsTrue) <p>User is active</p>$else <p>Not active</p>$endif", " <p>User is active</p>")]
    // Проверка на блок if и вставка переменной
    [DataRow("$if(IsTrue) <p>${Name}</p>$endif", " <p>Aboba</p>")]
    // Проверка на блок if и перед ним вставка переменной
    [DataRow("${Email} $if(IsTrue) <p>${Name}</p>$endif", "test@test.ru  <p>Aboba</p>")]
    // Проверка на блок if и после него вставка переменной
    [DataRow("$if(IsTrue) <p>${Name}</p>$endif ${Email}", " <p>Aboba</p> test@test.ru")]
    // Проверка если False
    [DataRow("$if(IsFalse) <p>User is active</p>$endif ", " ")]
    // Проверка если False и сущствует else
    [DataRow("$if(IsFalse) <p>User is active</p>$else <p>Not active</p>$endif", " <p>Not active</p>")]
    // Проверка если False и внутри else существует вставка переменной
    [DataRow("$if(IsFalse) <p>User is active</p>$else <p>${Name}</p>$endif", " <p>Aboba</p>")]
    // Проверка на вложенность и вставка переменной
    [DataRow("$if(IsTrue)$if(IsTrue)${Name}$endif $endif", "Aboba ")]
    [DataRow("$if(IsTrue)$if(IsTrue)${Name}$else Net $endif $endif", "Aboba ")]
    [DataRow("$if(IsTrue)$if(IsFalse)${Name}$else Net $endif $endif", " Net  ")]
    [DataRow("$if(IsFalse)$if(IsFalse)${Name}$else Net $endif $endif", "")]
    [DataRow("$if(IsFalse)$if(IsFalse)${Name}$else Net $endif $else a $endif", " a ")]
    // Проверка на вложенность и вставка переменной внутри блока if и внутри ещё блок if
    [DataRow("$if(IsTrue)${Email} $if(IsFalse)${Name}$else Net $endif $else a $endif", "test@test.ru  Net  ")]
    public void RenderFromString_IfInTemplate_ReturnCorrectString(string templateHtml, string expected)
    {
        // Arrange
        var testee = new HtmlTemplateRenderer();
        var model = new
        {
            Name = "Aboba",
            Email = "test@test.ru",
            Group = new
            {
                Id = 1,
                Name = "11-409",
            },
            IsTrue = true,
            IsFalse = false,
        };

        // Act
        var result = testee.RenderFromString(templateHtml, model);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    // Проверка на foreach
    [DataRow("$foreach(var item in Items) <li>${item.Name}</li> $endfor", " <li>Item 1</li>  <li>Item 2</li> ")]
    // Проверка на пустую коллекцию в foreach
    [DataRow("$foreach(var item in EmptyItems) <li>${item.Name}</li> $endfor", "")]
    // Проверка на вставку переменной перед, после и внутри.
    [DataRow("${Name} $foreach(var item in Items) <li>${item.Name}</li> $endfor", "Aboba  <li>Item 1</li>  <li>Item 2</li> ")]
    [DataRow("$foreach(var item in Items) <li>${item.Name}</li> $endfor ${Name}", " <li>Item 1</li>  <li>Item 2</li>  Aboba")]
    [DataRow("$foreach(var item in Items) <li>${item.Name}</li><p>${Name}</p> $endfor", " <li>Item 1</li><p>Aboba</p>  <li>Item 2</li><p>Aboba</p> ")]
    public void RenderFromString_ForeachInTemplate_ReturnCorrectString(string templateHtml, string expected)
    {
        // Arrange
        var testee = new HtmlTemplateRenderer();
        var model = new
        {
            Name = "Aboba",
            Email = "test@test.ru",
            Group = new
            {
                Id = 1,
                Name = "11-409",
            },
            IsTrue = true,
            IsFalse = false,
            Items = new[] {
                new { Name = "Item 1", Email = "Aboba@gmail.com" },
                new { Name = "Item 2", Email = "Biba@gmal.com" },
            },
            EmptyItems = new List<int>(),
        };

        // Act
        var result = testee.RenderFromString(templateHtml, model);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    // Проверка на if внутри foreach
    [DataRow("$foreach(var item in Items) $if(IsTrue)<li>${item.Name}</li>$endif $endfor", " <li>Item 1</li>  <li>Item 2</li> ")]
    // Проверка на foreach внутри if
    [DataRow("$if(IsTrue)$foreach(var item in Items)<li>${item.Name}</li>$endfor $endif", "<li>Item 1</li><li>Item 2</li> ")]
    public void RenderFromString_ForeachAndIfInTemplate_ReturnCorrectString(string templateHtml, string expected)
    {
        // Arrange
        var testee = new HtmlTemplateRenderer();
        var model = new
        {
            Name = "Aboba",
            Email = "test@test.ru",
            Group = new
            {
                Id = 1,
                Name = "11-409",
            },
            IsTrue = true,
            IsFalse = false,
            Items = new[] {
                new { Name = "Item 1", Email = "Aboba@gmail.com" },
                new { Name = "Item 2", Email = "Biba@gmal.com" },
            },
        };

        // Act
        var result = testee.RenderFromString(templateHtml, model);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
