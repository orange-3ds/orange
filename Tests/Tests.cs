using OrangeLib.Info;

namespace Tests
{
    public class Utils
    {
        [Fact]
        public void TestRunCommandSafe()
        {
            string output = OrangeLib.Utils.RunCommandSafe("echo hi");
            Assert.Equal("hi", output);
        }
        [Fact]
        public void TestRunCommandStreamOutputChecked()
        {
            Assert.True(OrangeLib.Utils.RunCommandStreamOutputChecked("echo hi"));
        }
    }
    public class Info
    {
        [Fact]
        public void TestInfo()
        {
            var packageinfo = new PackageInfo();
            string examplepackage = @"[info]
Title: 3dslib
Description: 3ds library template.
Author: Zachary Jones
README: # 3dslib

This is a template for starting new 3DS library projects with the Orange package manager.

This uses Devkitpro. yn like everything else does...


[dependencies]
package.zip
";
            // write examplepackage to example info
            File.WriteAllText("examplepkg.cfg", examplepackage);
            var package = packageinfo.LoadCfg("examplepkg.cfg");
            Assert.Equal("3dslib", package.Title);
            Assert.Equal("Zachary Jones", package.Author);
            File.Delete("examplepkg.cfg");
        }
    }
}
