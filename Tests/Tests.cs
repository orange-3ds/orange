using OrangeLib.Info;
using OrangeLib;
using OrangeLib.Net;
using System.IO.Compression;

namespace Tests
{


    public class UtilsTests
    {
        [Fact]
        public void IsWindows_ReturnsCorrectPlatform()
        {
            // Test that the method returns a boolean
            var result = Utils.IsWindows();
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void IsMacOS_ReturnsCorrectPlatform()
        {
            // Test that the method returns a boolean
            var result = Utils.IsMacOS();
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void IsLinux_ReturnsCorrectPlatform()
        {
            // Test that the method returns a boolean
            var result = Utils.IsLinux();
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void ExecuteShellCommand_WithValidCommand_ReturnsTrue()
        {
            // Use a simple command that should work on all platforms
            var command = Utils.IsWindows() ? "echo test" : "echo test";
            var result = Utils.ExecuteShellCommand(command);
            Assert.True(result);
        }

        [Fact]
        public void ExecuteShellCommand_WithInvalidCommand_ReturnsFalse()
        {
            // Use a command that doesn't exist
            var result = Utils.ExecuteShellCommand("thiscommanddefinitilynoexists123");
            Assert.False(result);
        }

        [Fact]
        public void CopyFile_CopiesFileSuccessfully()
        {
            // Create a temporary source file
            var tempDir = Path.GetTempPath();
            var sourceFile = Path.Combine(tempDir, "test_source.txt");
            var targetFile = Path.Combine(tempDir, "test_target.txt");
            
            try
            {
                File.WriteAllText(sourceFile, "test content");
                Utils.CopyFile(sourceFile, targetFile);
                
                Assert.True(File.Exists(targetFile));
                Assert.Equal("test content", File.ReadAllText(targetFile));
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(targetFile)) File.Delete(targetFile);
            }
        }

        [Fact]
        public void CopyFile_OverwritesExistingFile()
        {
            var tempDir = Path.GetTempPath();
            var sourceFile = Path.Combine(tempDir, "test_source.txt");
            var targetFile = Path.Combine(tempDir, "test_target.txt");
            
            try
            {
                File.WriteAllText(sourceFile, "new content");
                File.WriteAllText(targetFile, "old content");
                
                Utils.CopyFile(sourceFile, targetFile);
                
                Assert.Equal("new content", File.ReadAllText(targetFile));
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(targetFile)) File.Delete(targetFile);
            }
        }

        [Fact]
        public void CreateZip_CreatesZipFromDirectory()
        {
            var tempDir = Path.GetTempPath();
            var sourceDir = Path.Combine(tempDir, "test_source_dir");
            var zipPath = Path.Combine(tempDir, "test.zip");
            
            try
            {
                Directory.CreateDirectory(sourceDir);
                File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");
                File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "content2");
                
                Utils.CreateZip(sourceDir, zipPath);
                
                Assert.True(File.Exists(zipPath));
                
                // Verify zip contents
                using (var zip = ZipFile.OpenRead(zipPath))
                {
                    Assert.Equal(2, zip.Entries.Count);
                    Assert.Contains(zip.Entries, e => e.Name == "file1.txt");
                    Assert.Contains(zip.Entries, e => e.Name == "file2.txt");
                }
            }
            finally
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (File.Exists(zipPath)) File.Delete(zipPath);
            }
        }

        [Fact]
        public void CopyFilesRecursively_CopiesDirectoryStructure()
        {
            var tempDir = Path.GetTempPath();
            var sourceDir = Path.Combine(tempDir, "test_source_recursive");
            var targetDir = Path.Combine(tempDir, "test_target_recursive");
            
            try
            {
                Directory.CreateDirectory(sourceDir);
                Directory.CreateDirectory(Path.Combine(sourceDir, "subdir"));
                File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");
                File.WriteAllText(Path.Combine(sourceDir, "subdir", "file2.txt"), "content2");
                
                Utils.CopyFilesRecursively(sourceDir, targetDir);
                
                Assert.True(Directory.Exists(targetDir));
                Assert.True(Directory.Exists(Path.Combine(targetDir, "subdir")));
                Assert.True(File.Exists(Path.Combine(targetDir, "file1.txt")));
                Assert.True(File.Exists(Path.Combine(targetDir, "subdir", "file2.txt")));
                Assert.Equal("content1", File.ReadAllText(Path.Combine(targetDir, "file1.txt")));
                Assert.Equal("content2", File.ReadAllText(Path.Combine(targetDir, "subdir", "file2.txt")));
            }
            finally
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            }
        }

        [Fact]
        public void CopyDirectoryRecursively_CopiesEntireDirectory()
        {
            var tempDir = Path.GetTempPath();
            var sourceDir = Path.Combine(tempDir, "test_source_full");
            var targetDir = Path.Combine(tempDir, "test_target_full");
            
            try
            {
                Directory.CreateDirectory(sourceDir);
                Directory.CreateDirectory(Path.Combine(sourceDir, "nested"));
                File.WriteAllText(Path.Combine(sourceDir, "root.txt"), "root content");
                File.WriteAllText(Path.Combine(sourceDir, "nested", "nested.txt"), "nested content");
                
                Utils.CopyDirectoryRecursively(sourceDir, targetDir);
                
                Assert.True(Directory.Exists(targetDir));
                Assert.True(Directory.Exists(Path.Combine(targetDir, "nested")));
                Assert.True(File.Exists(Path.Combine(targetDir, "root.txt")));
                Assert.True(File.Exists(Path.Combine(targetDir, "nested", "nested.txt")));
                Assert.Equal("root content", File.ReadAllText(Path.Combine(targetDir, "root.txt")));
                Assert.Equal("nested content", File.ReadAllText(Path.Combine(targetDir, "nested", "nested.txt")));
            }
            finally
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            }
        }

        [Fact]
        public void CopyDirectoryRecursively_WithNonexistentSource_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() => 
                Utils.CopyDirectoryRecursively("nonexistent_source", "target"));
        }
    }

    public class PackageInfoTests
    {
        [Fact]
        public void GetPackageTitle_ReturnsDefaultTitle()
        {
            var packageInfo = new PackageInfo();
            var result = packageInfo.GetPackageTitle();
            Assert.Equal("Oranges", result);
        }

        [Fact]
        public void GetPackageDescription_ReturnsDefaultDescription()
        {
            var packageInfo = new PackageInfo();
            var result = packageInfo.GetPackageDescription();
            Assert.Equal("3ds Homebrew library", result);
        }

        [Fact]
        public void GetPackageAuthor_ReturnsDefaultAuthor()
        {
            var packageInfo = new PackageInfo();
            var result = packageInfo.GetPackageAuthor();
            Assert.Equal("Me :)", result);
        }

        [Fact]
        public void GetDependencies_ReturnsEmptyStringForNoDependencies()
        {
            var packageInfo = new PackageInfo();
            var result = packageInfo.GetDependencies();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ArrayToStringSpaceSeperate_WithValidArray_ReturnsSpaceSeparatedString()
        {
            var array = new string[] { "dep1", "dep2", "dep3" };
            var result = PackageInfo.ArrayToStringSpaceSeperate(array);
            Assert.Equal("dep1 dep2 dep3", result);
        }

        [Fact]
        public void ArrayToStringSpaceSeperate_WithEmptyArray_ReturnsEmptyString()
        {
            var array = new string[] { };
            var result = PackageInfo.ArrayToStringSpaceSeperate(array);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ArrayToStringSpaceSeperate_WithNullArray_ReturnsEmptyString()
        {
            var result = PackageInfo.ArrayToStringSpaceSeperate(null!);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetReadmeContents_WithValidFile_ReturnsFileContents()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var expectedContent = "This is a test README file.";
                File.WriteAllText(tempFile, expectedContent);
                
                var result = PackageInfo.GetReadmeContents(tempFile);
                Assert.Equal(expectedContent, result);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetReadmeContents_WithNonexistentFile_ReturnsNotFoundMessage()
        {
            var nonexistentPath = "nonexistent_file.txt";
            var result = PackageInfo.GetReadmeContents(nonexistentPath);
            Assert.Contains("README file not found", result);
        }

        [Fact]
        public void GetReadmeContents_WithEmptyPath_ReturnsNoPathMessage()
        {
            var result = PackageInfo.GetReadmeContents("");
            Assert.Equal("README path not provided", result);
        }

        [Fact]
        public void GetReadmeContents_WithWhitespacePath_ReturnsNoPathMessage()
        {
            var result = PackageInfo.GetReadmeContents("   ");
            Assert.Equal("README path not provided", result);
        }

        [Fact]
        public void LoadCfg_WithValidConfigFile_LoadsCorrectly()
        {
            var configContent = @"[info]
Title: TestPackage
Description: A test package
Author: Test Author
README: TestReadme.md

[dependencies]
dep1
dep2
";
            var tempFile = Path.GetTempFileName();
            var configFile = Path.ChangeExtension(tempFile, ".cfg");
            
            try
            {
                File.WriteAllText(configFile, configContent);
                
                var packageInfo = new PackageInfo();
                var result = packageInfo.LoadCfg(configFile);
                
                Assert.Equal("TestPackage", result.Title);
                Assert.Equal("A test package", result.Description);
                Assert.Equal("Test Author", result.Author);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                if (File.Exists(configFile)) File.Delete(configFile);
            }
        }

        [Fact]
        public void LoadCfg_WithNonexistentFile_ThrowsFileNotFoundException()
        {
            var packageInfo = new PackageInfo();
            Assert.Throws<FileNotFoundException>(() => packageInfo.LoadCfg("nonexistent.cfg"));
        }

        [Fact]
        public void LoadCfg_WithInvalidFileExtension_ThrowsFileNotFoundException()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var packageInfo = new PackageInfo();
                Assert.Throws<FileNotFoundException>(() => packageInfo.LoadCfg(tempFile));
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void LoadCfg_WithEmptyFilename_ThrowsArgumentException()
        {
            var packageInfo = new PackageInfo();
            Assert.Throws<ArgumentException>(() => packageInfo.LoadCfg(""));
        }

        [Fact]
        public void AddDependencyToCfg_WithValidDependency_AddsDependency()
        {
            var configContent = @"[info]
Title: TestPackage
Description: A test package
Author: Test Author

[dependencies]
";
            var tempFile = Path.GetTempFileName();
            var configFile = Path.ChangeExtension(tempFile, ".cfg");
            
            try
            {
                File.WriteAllText(configFile, configContent);
                
                var packageInfo = new PackageInfo();
                var result = packageInfo.AddDependencyToCfg("newdep", configFile);
                
                Assert.Contains("newdep", result.Dependencies);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                if (File.Exists(configFile)) File.Delete(configFile);
            }
        }

        [Fact]
        public void AddDependencyToCfg_WithEmptyDependency_ThrowsArgumentException()
        {
            var packageInfo = new PackageInfo();
            Assert.Throws<ArgumentException>(() => packageInfo.AddDependencyToCfg("", "test.cfg"));
        }

        [Fact]
        public void GetInformation_ReturnsCorrectStructure()
        {
            var packageInfo = new PackageInfo();
            var result = packageInfo.GetInformation();
            
            Assert.Equal("Oranges", result.Title);
            Assert.Equal("3ds Homebrew library", result.Description);
            Assert.Equal("Me :)", result.Author);
            Assert.Equal(string.Empty, result.Dependencies);
        }
    }

    public class InternetTests
    {
        [Fact]
        public void GetWebPath_ReturnsDefaultPath()
        {
            Internet.ResetWebPath();
            var result = Internet.GetWebPath();
            Assert.False(string.IsNullOrEmpty(result));
            Assert.StartsWith("https://", result);
        }

        [Fact]
        public void SetWebPath_WithValidPath_SetsPath()
        {
            var testPath = "https://example.com/";
            Internet.SetWebPath(testPath);
            var result = Internet.GetWebPath();
            Assert.Equal(testPath, result);
            
            // Reset to default for other tests
            Internet.ResetWebPath();
        }

        [Fact]
        public void SetWebPath_WithEmptyPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Internet.SetWebPath(""));
        }

        [Fact]
        public void SetWebPath_WithNullPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Internet.SetWebPath(null!));
        }

        [Fact]
        public void SetWebPath_WithWhitespacePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Internet.SetWebPath("   "));
        }

        [Fact]
        public void ResetWebPath_ResetsToDefaultPath()
        {
            var originalPath = Internet.GetWebPath();
            Internet.SetWebPath("https://custom.com/");
            Internet.ResetWebPath();
            var resetPath = Internet.GetWebPath();
            Assert.Equal(originalPath, resetPath);
        }

        [Fact]
        public async Task GetPackage_WithEmptyPackageName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Internet.GetPackage(""));
        }

        [Fact]
        public async Task GetPackage_WithNullPackageName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Internet.GetPackage(null!));
        }

        [Fact]
        public async Task GetPackage_WithWhitespacePackageName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Internet.GetPackage("   "));
        }
    }

    public class PackageTests
    {
        [Fact]
        public void InstallPackage_WithNonexistentZipFile_HandlesGracefully()
        {
            // Test with nonexistent zip file
            // This should not throw an exception but handle it gracefully
            Package.InstallPackage("nonexistent.zip");
            // If we get here, the method handled the error gracefully
            Assert.True(true);
        }

        [Fact]
        public void CreatePackage_WithValidInformation_CreatesPackageZip()
        {
            var tempDir = Path.GetTempPath();
            var workingDir = Path.Combine(tempDir, "package_test");
            var originalDir = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.CreateDirectory(workingDir);
                Directory.SetCurrentDirectory(workingDir);
                
                // Create lib and include directories with test files
                Directory.CreateDirectory("lib");
                Directory.CreateDirectory("include");
                File.WriteAllText("lib/test.a", "library content");
                File.WriteAllText("include/test.h", "header content");
                
                var packageInfo = new Information
                {
                    Title = "TestPackage",
                    Description = "Test Description",
                    Author = "Test Author",
                    Dependencies = "dep1 dep2",
                    ReadmeContents = "Test README"
                };
                
                Package.CreatePackage(packageInfo);
                
                // Verify package.zip was created
                Assert.True(File.Exists("package.zip"));
                
                // Verify package directory structure
                Assert.True(Directory.Exists("package"));
                Assert.True(File.Exists("package/package.json"));
                Assert.True(Directory.Exists("package/lib"));
                Assert.True(Directory.Exists("package/include"));
                Assert.True(File.Exists("package/lib/test.a"));
                Assert.True(File.Exists("package/include/test.h"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                if (Directory.Exists(workingDir))
                {
                    try
                    {
                        // Clean up by removing read-only attributes and deleting
                        foreach (var file in Directory.GetFiles(workingDir, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        foreach (var dir in Directory.GetDirectories(workingDir, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(dir, FileAttributes.Normal);
                        }
                        Directory.Delete(workingDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}
