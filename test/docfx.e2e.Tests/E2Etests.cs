// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.E2E.tests
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using global::Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.Hosting;
    using Microsoft.Owin.StaticFiles;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Firefox;
    using Xunit;

    public class E2Etests
    {
        IWebDriver driver;

        public E2Etests()
        {
            driver = new FirefoxDriver();
            driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(1));
            var fileServerOptions = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = new PhysicalFileSystem("..\\..\\..\\..\\Documentation\\_site"),
            };
            try
            {
                WebApp.Start("http://localhost:8080", builder => builder.UseFileServer(fileServerOptions));
            }
            catch (Exception)
            {
                // Assume that server is already running on machine for docfx documentation site.
            }
        }

        ~E2Etests()
        {
            driver.Quit();
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestDefaultTemplate_HomePage()
        {
            driver.Navigate().GoToUrl("http://localhost:8080/index.html");

            // check title
            Assert.Equal("Welcome to Docfx website! | docfx website", driver.Title);

            // check logo
            IWebElement element = driver.FindElement(By.Id("logo"));
            Assert.Equal(0, element.Location.Y);
            Assert.Equal("svg", element.TagName);

            // check navbar
            driver.FindElement(By.LinkText("Tutorials")).Click();
            Assert.Equal("Getting Started with docfx | docfx website", driver.Title);
            driver.FindElement(By.LinkText("Guidelines")).Click();
            Assert.Equal("Engineering Guidelines | docfx website", driver.Title);
            driver.FindElement(By.LinkText("Specifications")).Click();
            Assert.Equal("Doc-as-Code: Metadata Format Specification | docfx website", driver.Title);
            driver.FindElement(By.LinkText("API Documentation")).Click();
            Assert.Equal("Namespace IronRuby.Builtins | docfx website", driver.Title);
            driver.FindElement(By.Id("logo")).Click();
            Assert.Equal("Welcome to Docfx website! | docfx website", driver.Title);

            // check text
            Assert.Equal("DOCFX", driver.FindElement(By.ClassName("text")).Text);

            // check minitext
            Assert.Equal("A documentation generation tool for API reference and markdown files!".ToUpper(),
                driver.FindElement(By.ClassName("minitext")).Text);

            // check "Get Started" button
            driver.FindElement(By.LinkText("Get Started")).Click();
            Assert.Equal("Getting Started with docfx | docfx website", driver.Title);
            driver.Navigate().Back();

            // check "Download Lastest Docfx!" button
            var zipUrl = driver.FindElement(By.LinkText("Download Latest Docfx!")).GetAttribute("href");
            WebRequest request = WebRequest.Create(zipUrl);
            Assert.Contains("zip", request.GetResponse().ContentType);

            // check content
            IList<IWebElement> listFindResult = driver.FindElements(By.XPath("//div[@class='row value-props']/div/strong"));
            Assert.Equal("API-Documentation", listFindResult[0].Text);
            Assert.Equal("Markdown-Documentation", listFindResult[1].Text);
            Assert.Equal("Customize", listFindResult[2].Text);
            listFindResult = driver.FindElements(By.XPath("//div[@class='row value-props']/div/p"));
            Assert.Equal("Able to generate API documentation from triple-slash-comments for .NET source code directly!", listFindResult[0].Text);
            Assert.Equal("Able to generate HTML from markdown files supporting DFM syntax.", listFindResult[1].Text);
            Assert.Equal("Able to customize templates and themes easily", listFindResult[2].Text);

            // check footer
            Assert.Contains("Copyright © 2015 Microsoft", driver.PageSource);
            Assert.Contains("Powered by Doc-as-Code", driver.PageSource);
            driver.FindElement(By.LinkText("Back to top")).Click();
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestDefaultTemplate_ConceptionPage()
        {
            driver.Navigate().GoToUrl("http://localhost:8080/index.html");

            // check title
            driver.FindElement(By.LinkText("Tutorials")).Click();
            Assert.Equal("Getting Started with docfx | docfx website", driver.Title);

            // check breadcrumb
            IList<IWebElement> listFindResult = driver.FindElements(By.XPath("//div[@id='breadcrumb']/ul/li/a"));
            Assert.Equal("Tutorials", listFindResult[0].Text);
            listFindResult[0].Click();
            Assert.Equal("Getting Started with docfx | docfx website", driver.Title);
            listFindResult = driver.FindElements(By.XPath("//div[@id='breadcrumb']/ul/li/a"));
            Assert.Equal("Getting Started", listFindResult[1].Text);
            listFindResult[1].Click();
            Assert.Equal("Getting Started with docfx | docfx website", driver.Title);

            // check toc
            driver.SwitchTo().Frame("sidetoc");
            listFindResult = driver.FindElements(By.XPath("//div[@id='toc']/ul/li/a"));
            Assert.Equal("Getting Started", listFindResult[0].Text);
            Assert.Equal("User Manual", listFindResult[1].Text);
            Assert.Equal("How To Create Custom Template", listFindResult[2].Text);
            driver.FindElement(By.Id("toc_filter_input")).SendKeys("user");
            Assert.False(listFindResult[0].Displayed);
            Assert.True(listFindResult[1].Displayed);
            Assert.False(listFindResult[2].Displayed);
            listFindResult[1].Click();
            Assert.Equal("Doc-as-code: docfx.exe User Manual | docfx website", driver.Title);
            driver.Navigate().Back();

            // check sidebar
            listFindResult = driver.FindElements(By.XPath("//nav[@id='affix']/ul/li/a"));
            Assert.Equal("Getting Started", listFindResult[0].Text);
            Assert.Equal("Use docfx.exe directly", listFindResult[1].Text);
            Assert.Equal("Adding markdown to API reference", listFindResult[5].Text);
            listFindResult[1].Click();
            IWebElement element = driver.FindElement(By.XPath("//nav[@id='affix']/ul/li[2]/ul/li/a"));
            Assert.True(element.Displayed);
            element.Click();

            // check article title
            listFindResult = driver.FindElements(By.TagName("h1"));
            Assert.Equal("Getting Started with docfx", listFindResult[0].Text);
            listFindResult = driver.FindElements(By.TagName("h2"));
            Assert.Equal("Getting Started", listFindResult[0].Text);
            Assert.Equal("Use docfx.exe directly", listFindResult[1].Text);
            Assert.Equal("Adding markdown to API reference", listFindResult[5].Text);
            listFindResult = driver.FindElements(By.TagName("h3"));
            Assert.Equal("Quick Start", listFindResult[0].Text);
            Assert.Equal("Quick Start", listFindResult[1].Text);
            Assert.Equal("Linking to another API", listFindResult[4].Text);

            // check article link
            driver.FindElement(By.XPath("//nav[@id='affix']/ul/li/a[@href='#getting-started']")).Click();
            driver.FindElement(By.LinkText("how to create custom template")).Click();
            Assert.Equal("How-to: Create Custom Templates | docfx website", driver.Title);
            driver.Navigate().Back();
            driver.FindElement(By.LinkText("DFM")).Click();
            Assert.Equal("Docfx Flavored Markdown | docfx website", driver.Title);
            driver.Navigate().Back();
            driver.FindElement(By.XPath("//nav[@id='affix']/ul/li/a[@href='#use-docfx-under-visual-studio-ide']")).Click();
            driver.FindElement(By.LinkText("Visual Studio 2015")).Click();
            Assert.Contains("Visual Studio", driver.Title);
            driver.Navigate().Back();
            driver.FindElement(By.XPath("//nav[@id='affix']/ul/li/a[@href='#use-docfx-under-dnx']")).Click();
            driver.FindElement(By.LinkText("DNVM")).Click();
            Assert.Contains("DNVM", driver.PageSource);
            driver.Navigate().Back();
        }


        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestDefaultTemplate_ReferencePage()
        {
            driver.Navigate().GoToUrl("http://localhost:8080/index.html");

            // check title
            driver.FindElement(By.LinkText("API Documentation")).Click();
            Assert.Equal("Namespace IronRuby.Builtins | docfx website", driver.Title);

            // check toc
            driver.SwitchTo().Frame("sidetoc");
            driver.FindElement(By.LinkText("Microsoft.DocAsCode")).Click();
            Assert.Equal("Namespace Microsoft.DocAsCode | docfx website", driver.Title);
            driver.SwitchTo().Frame("sidetoc");
            driver.FindElement(By.LinkText("Microsoft.DocAsCode.EntityModel")).Click();
            Assert.Equal("Namespace Microsoft.DocAsCode.EntityModel | docfx website", driver.Title);
            driver.SwitchTo().Frame("sidetoc");
            driver.FindElement(By.LinkText("Microsoft.DocAsCode.Utility")).Click();
            Assert.Equal("Namespace Microsoft.DocAsCode.Utility | docfx website", driver.Title);
            driver.SwitchTo().Frame("sidetoc");
            driver.FindElement(By.Id("toc_filter_input")).SendKeys("yamlutility");
            driver.FindElement(By.LinkText("YamlUtility")).Click();
            Assert.Equal("Class YamlUtility | docfx website", driver.Title);

            // check link
            driver.FindElement(By.LinkText("Reference Source")).Click();
            Assert.Equal("Reference Source", driver.Title);
            driver.Navigate().Back();

            // check Xref
            driver.FindElement(By.LinkText("ProjectLevelCache")).Click();
            Assert.Equal("Class ProjectLevelCache | docfx website", driver.Title);
            driver.Navigate().Back();
            Assert.Equal("Files", driver.FindElement(By.XPath("//article[@id='_content']/div[2]/p[2]/strong")).Text);

            // check content
            Assert.Equal("Class YamlUtility", driver.FindElement(By.TagName("h1")).Text);
            Assert.Equal("Methods", driver.FindElement(By.TagName("h3")).Text);
            IList<IWebElement> listFindResult = driver.FindElements(By.TagName("h4"));
            Assert.Equal("Deserialize<T>(TextReader)", listFindResult[0].Text);
            Assert.Equal("Deserialize<T>(String)", listFindResult[1].Text);
            Assert.Equal("Serialize(TextWriter, Object)", listFindResult[2].Text);
            Assert.Equal("Serialize(String, Object)", listFindResult[3].Text);
            listFindResult = driver.FindElements(By.XPath("//article[@id='_content']/div/pre/code"));
            Assert.Equal("public class YamlUtility", listFindResult[0].Text);
            Assert.Equal("public static T Deserialize<T>(TextReader reader)", listFindResult[1].Text);
            Assert.Equal("public static T Deserialize<T>(string path)", listFindResult[2].Text);
            Assert.Equal("public static void Serialize(TextWriter writer, object graph)", listFindResult[3].Text);
            Assert.Equal("public static void Serialize(string path, object graph)", listFindResult[4].Text);
            listFindResult = driver.FindElements(By.XPath("//article[@id='_content']/table[6]/tbody/tr/td[1]/span"));
            Assert.Equal("System.String", listFindResult[0].Text);
            Assert.Equal("System.Object", listFindResult[1].Text);
            listFindResult = driver.FindElements(By.XPath("//article[@id='_content']/table[6]/tbody/tr/td[2]/em"));
            Assert.Equal("path", listFindResult[0].Text);
            Assert.Equal("graph", listFindResult[1].Text);

            // check sidebar
            IWebElement element = driver.FindElement(By.XPath("//nav[@id='affix']/ul/li/a"));
            Assert.Equal("Methods", element.Text);
            element.Click();
            listFindResult = driver.FindElements(By.XPath("//nav[@id='affix']/ul/li/ul/li/a"));
            Assert.True(listFindResult[0].Displayed);
            Assert.True(listFindResult[1].Displayed);
            Assert.True(listFindResult[2].Displayed);
            Assert.True(listFindResult[3].Displayed);
            listFindResult[0].Click();

            // check breadcrumb
            listFindResult = driver.FindElements(By.XPath("//div[@id='breadcrumb']/ul/li/a"));
            Assert.Equal("API Documentation", listFindResult[0].Text);
            Assert.Equal("Microsoft.DocAsCode.EntityModel", listFindResult[1].Text);
            Assert.Equal("YamlUtility", listFindResult[2].Text);
            listFindResult[2].Click();
            Assert.Equal("Class YamlUtility | docfx website", driver.Title);
            driver.FindElement(By.XPath("//div[@id='breadcrumb']/ul/li[2]/a")).Click();
            Assert.Equal("Namespace Microsoft.DocAsCode.EntityModel | docfx website", driver.Title);
            Assert.Equal(3, driver.FindElements(By.TagName("h3")).Count);
            driver.FindElement(By.XPath("//div[@id='breadcrumb']/ul/li[1]/a")).Click();
            Assert.Equal("Namespace IronRuby.Builtins | docfx website", driver.Title);
        }
    }
}