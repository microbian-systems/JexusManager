﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Tests.Exceptions
{
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Microsoft.Web.Administration;
    using Xunit;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using System.Net;

    public class ServerTestCases
    {
        [Fact]
        public void TestProviders()
        {
            const string current = @"applicationHost.config";
            const string original = @"original2.config";
            const string originalMono = @"original.mono.config";
            File.Copy(Helper.IsRunningOnMono() ? originalMono : original, current, true);

            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }
#if IIS
            var server = new ServerManager(Path.Combine(directoryName, current));
#else
            var server = new IisExpressServerManager(Path.Combine(directoryName, current));
#endif
            var config = server.GetApplicationHostConfiguration();
            var section = config.GetSection("configProtectedData");
            Assert.Equal("RsaProtectedConfigurationProvider", section["defaultProvider"]);
            var collection = section.GetCollection("providers");
            Assert.Equal(5, collection.Count);
        }

        [Fact]
        public void TestIisExpressMissingFile()
        {
            const string original = @"applicationHost.config";
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            var file = Path.Combine(directoryName, original);
            File.Delete(file);

#if IIS
            var server = new ServerManager(file);
#else
            var server = new IisExpressServerManager(file);
#endif
            var exception = Assert.Throws<FileNotFoundException>(
                () =>
                    {
                        TestCases.TestIisExpress(server);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{file}\r\nError: Cannot read configuration file\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void TestIisExpressMissingClosingTag()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(@"original2_missing_closing.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.TestIisExpress(server);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 1135\r\nError: Configuration file is not well-formed XML\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void TestIisExpressMissingRequiredAttribute()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // Remove the attribute.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pool = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools/add[@name='UnmanagedClassicAppPool']");
                pool?.SetAttributeValue("name", null);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.TestIisExpress(server);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 142\r\nError: Missing required attribute 'name'\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void TestIisExpressValidatorFails()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // Remove the attribute.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pool = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools/add[@name='UnmanagedClassicAppPool']");
                pool?.SetAttributeValue("name", string.Empty);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.TestIisExpress(server);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 142\r\nError: The 'name' attribute is invalid.  Invalid application pool name\r\n\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void TestIisExpressInvalidAttribute()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // Remove the attribute.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pool = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools/add[@name='UnmanagedClassicAppPool']");
                pool?.SetAttributeValue("testAuto", true);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.TestIisExpress(server);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 142\r\nError: Unrecognized attribute 'testAuto'\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void TestIisExpressReadOnly()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            var message =
                "The configuration object is read only, because it has been committed by a call to ServerManager.CommitChanges(). If write access is required, use ServerManager to get a new reference.";
#if IIS
            var server = new ServerManager(true, current);
#else
            var server = new IisExpressServerManager(true, current);
#endif
            var exception1 = Assert.Throws<InvalidOperationException>(
                () =>
                    {
                        TestCases.TestIisExpress(server);
                    });
            Assert.Equal(message, exception1.Message);

            // site config "Website1"
            var config = server.Sites[0].Applications[0].GetWebConfiguration();

            // enable Windows authentication
            var windowsSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication");
            Assert.Equal(OverrideMode.Inherit, windowsSection.OverrideMode);
            Assert.Equal(OverrideMode.Deny, windowsSection.OverrideModeEffective);
            Assert.True(windowsSection.IsLocked);
            Assert.False(windowsSection.IsLocallyStored);

            var windowsEnabled = (bool)windowsSection["enabled"];
            Assert.False(windowsEnabled);

            var compression = config.GetSection("system.webServer/urlCompression");
            Assert.Equal(OverrideMode.Inherit, compression.OverrideMode);
            Assert.Equal(OverrideMode.Allow, compression.OverrideModeEffective);
            Assert.False(compression.IsLocked);
            Assert.False(compression.IsLocallyStored);

            Assert.Equal(true, compression["doDynamicCompression"]);

            var compress = Assert.Throws<InvalidOperationException>(() => compression["doDynamicCompression"] = false);
            Assert.Equal(message, compress.Message);

            {
                // disable default document. Saved to web.config as this section can be overridden anywhere.
                ConfigurationSection defaultDocumentSection = config.GetSection("system.webServer/defaultDocument");
                Assert.Equal(true, defaultDocumentSection["enabled"]);

                ConfigurationElementCollection filesCollection = defaultDocumentSection.GetCollection("files");
                Assert.Equal(7, filesCollection.Count);

                {
                    var first = filesCollection[0];
                    Assert.Equal("home1.html", first["value"]);
                    Assert.True(first.IsLocallyStored);
                }

                var second = filesCollection[1];
                Assert.Equal("Default.htm", second["value"]);
                Assert.False(second.IsLocallyStored);

                var third = filesCollection[2];
                Assert.Equal("Default.asp", third["value"]);
                Assert.False(third.IsLocallyStored);

                var remove = Assert.Throws<FileLoadException>(() => filesCollection.RemoveAt(4));
                Assert.Equal(
                    "Filename: \r\nError: This configuration section cannot be modified because it has been opened for read only access\r\n\r\n",
                    remove.Message);

                ConfigurationElement addElement = filesCollection.CreateElement();
                var add = Assert.Throws<InvalidOperationException>(() => filesCollection.AddAt(0, addElement));
                Assert.Equal(message, add.Message);

                Assert.Equal(7, filesCollection.Count);

                {
                    var first = filesCollection[0];
                    Assert.Equal("home1.html", first["value"]);
                    // TODO: why?
                    // Assert.IsFalse(first.IsLocallyStored);
                }

                Assert.Equal(7, filesCollection.Count);

                var clear = Assert.Throws<InvalidOperationException>(() => filesCollection.Clear());
                Assert.Equal(message, clear.Message);

                var delete = Assert.Throws<InvalidOperationException>(() => filesCollection.Delete());
                Assert.Equal(message, delete.Message);
            }
        }
        
        [Fact]
        public void TestIisExpressNoBinding()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // change the path.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var app = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/bindings");
                app.Remove();
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    var binding = site.Bindings[0];
                });
        }


        [Fact]
        public void TestIisExpressNoRootApplication()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // change the path.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var app = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application");
                app?.SetAttributeValue("path", "/xxx");
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            var exception = Assert.Throws<FileNotFoundException>(
                () =>
                {
                    var root = config.RootSectionGroup;
                });
#if IIS
            Assert.Equal(string.Format("Filename: \\\\?\\{0}\r\nError: Unrecognized configuration path 'MACHINE/WEBROOT/APPHOST/WebSite1'\r\n\r\n", current), exception.Message);
#else
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 155\r\nError: Unrecognized configuration path 'MACHINE/WEBROOT/APPHOST/WebSite1'\r\n\r\n", exception.Message);
#endif
        }

        [Fact]
        public void TestIisExpressRootApplicationOutOfOrder()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var app = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application");
                app.AddBeforeSelf(
                    new XElement("application",
                        new XAttribute("path", "/xxx"),
                        new XElement("virtualDirectory",
                            new XAttribute("path", "/"),
                            new XAttribute("physicalPath", @"%JEXUS_TEST_HOME%\WebSite1"))));
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            {
                var root = config.RootSectionGroup;
                Assert.NotNull(root);
            }

            // enable Windows authentication
            var windowsSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication");
            Assert.Equal(OverrideMode.Inherit, windowsSection.OverrideMode);
            Assert.Equal(OverrideMode.Deny, windowsSection.OverrideModeEffective);
            Assert.True(windowsSection.IsLocked);
            Assert.False(windowsSection.IsLocallyStored);
        }

        [Fact]
        public void TestIisExpressNoRootVirtualDirectory()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // modify the path
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var vDir = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application/virtualDirectory");
                vDir?.SetAttributeValue("path", "/xxx");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
#if IIS
            var exception = Assert.Throws<NullReferenceException>(
                () =>
                {
                    var root = config.RootSectionGroup;
                });
#else
            var exception = Assert.Throws<FileNotFoundException>(
                () =>
                {
                    var root = config.RootSectionGroup;
                });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 156\r\nError: Unrecognized configuration path 'MACHINE/WEBROOT/APPHOST/WebSite1'\r\n\r\n", exception.Message);
#endif
        }

        [Fact]
        public void TestIisExpressRootVirtualDirectoryOutOfOrder()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // modify the path
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var vDir = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application/virtualDirectory");
                var newDir = new XElement("virtualDirectory",
                    new XAttribute("path", "/xxx"),
                    new XAttribute("physicalPath", @"%JEXUS_TEST_HOME%\WebSite1"));
                vDir.AddBeforeSelf(newDir);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            {
                var root = config.RootSectionGroup;
                Assert.NotNull(root);
            }

            // enable Windows authentication
            var windowsSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication");
            Assert.Equal(OverrideMode.Inherit, windowsSection.OverrideMode);
            Assert.Equal(OverrideMode.Deny, windowsSection.OverrideModeEffective);
            Assert.True(windowsSection.IsLocked);
            Assert.False(windowsSection.IsLocallyStored);
        }

        [Fact]
        public void TestIisExpressLogFileInheritance()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']");
                var log = new XElement("logFile",
                    new XAttribute("logFormat", "IIS"));
                site1?.Add(log);

                var site2 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='3']");
                var log2 = new XElement("logFile",
                    new XAttribute("directory", @"%IIS_USER_HOME%\Logs\1"));
                site2?.Add(log2);

                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                var site = server.Sites[0];
                Assert.Equal(@"%IIS_USER_HOME%\Logs", site.LogFile.Directory);
                Assert.Equal(LogFormat.W3c, site.LogFile.LogFormat);
            }

            {
                var site = server.Sites[1];
                Assert.Equal(@"%IIS_USER_HOME%\Logs", site.LogFile.Directory);
                Assert.Equal(LogFormat.Iis, site.LogFile.LogFormat);
            }

            {
                var site = server.Sites[2];
                Assert.Equal(@"%IIS_USER_HOME%\Logs\1", site.LogFile.Directory);
                Assert.Equal(LogFormat.W3c, site.LogFile.LogFormat);
            }
        }

        [Fact]
        public void TestIisExpressDuplicateBindings()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "*:61902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                var exception = Assert.Throws<COMException>(() => server.Sites[1]);
                Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 183\r\nError: Cannot add duplicate collection entry of type 'binding' with combined key attributes 'protocol, bindingInformation' respectively set to 'http, *:61902:localhost'\r\n\r\n", exception.Message);
            }
        }

        [Fact]
        public void TestIisExpressBindingInvalidPort()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "*:161902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Assert.Null(server.Sites[1].Bindings[2].EndPoint);
                Assert.Equal("*:161902:localhost", server.Sites[1].Bindings[2].BindingInformation);
            }
        }

        [Fact]
        public void TestIisExpressBindingInvalidAddress()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "1.1.1.1.1:61902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Null(binding.EndPoint);
                Assert.Equal("1.1.1.1.1:61902:localhost", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost:61902", binding.ToUri());
                Assert.Equal("localhost on 1.1.1.1.1:61902 (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void TestIisExpressBindingInvalidAddress2()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "1.1.1:61902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Equal(IPAddress.Parse("1.1.0.1"), binding.EndPoint.Address);
                Assert.Equal("1.1.1:61902:localhost", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost:61902", binding.ToUri());
                Assert.Equal("localhost on 1.1.1:61902 (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void TestIisExpressBindingInvalidAddress3()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", ":")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Null(binding.EndPoint);
                Assert.Equal(":", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost", binding.ToUri());
                Assert.Equal(": (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void TestIisExpressBindingInvalidAddress4()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "::")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Null(binding.EndPoint);
                Assert.Equal("::", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost", binding.ToUri());
                Assert.Equal(": (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void TestIisExpressDuplicateApplicationPools()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pools = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools");
                pools?.Add(
                    new XElement("add",
                        new XAttribute("name", "Clr4IntegratedAppPool")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                var exception = Assert.Throws<COMException>(() => server.ApplicationPools);
                Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 144\r\nError: Cannot add duplicate collection entry of type 'add' with unique key attribute 'name' set to 'Clr4IntegratedAppPool'\r\n\r\n", exception.Message);
            }
        }


        [Fact]
        public void TestIisExpressConfigSource()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            var siteConfig = TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                root.Add(
                    new XElement("location",
                        new XAttribute("path", "WebSite1"),
                        new XElement("system.web",
                            new XElement("authorization",
                                new XAttribute("configSource", Path.Combine(Path.GetDirectoryName(siteConfig), "authorization.config"))))));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif

#if IIS
            var config = server.Sites[0].Applications[0].GetWebConfiguration();
            var exception = Assert.Throws<COMException>(() => config.GetSection("system.web/authorization"));
#else
            // TODO: fix where the exception is throwed.
            var exception = Assert.Throws<COMException>(() => server.Sites[0].Applications[0].GetWebConfiguration());
#endif
            Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 1121\r\nError: Unrecognized attribute 'configSource'\r\n\r\n",
                exception.Message);
        }
    }
}
