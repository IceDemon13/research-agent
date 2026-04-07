using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Bogus;
using Humanizer;
using Telemart.Client.Data.Options;
using Telemart.Client.ViewModels.Content.ProductImages;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public sealed class ProductImagesDirectoryProcessorTests
    {
        private readonly ProductImageFileValidator productImageFileValidator = new ProductImageFileValidator(new FileUploadOptions{PhotoMaxSizeMb = 10});

        [Fact]
        public void CtorTest()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new ProductImagesDirectoryProcessor(null, null);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void DirectoryNameShoudNotBeNullOrEmpty(string directoryName)
        {
            ProductImagesDirectoryProcessor processor = new ProductImagesDirectoryProcessor(new MockFileSystem(), productImageFileValidator);

            Assert.Throws<ArgumentException>(() =>
            {
                processor.Process(directoryName).ToArray();
            });
        }

        [Fact]
        public void NonExistingDirectoryTest()
        {
            ProductImagesDirectoryProcessor processor = new ProductImagesDirectoryProcessor(new MockFileSystem(), productImageFileValidator);

            Assert.Empty(processor.Process(@"e:\temp\images"));
        }

        [Fact]
        public void EmptyDirectoryTest()
        {
            MockFileSystem mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"e:\temp\images");

            ProductImagesDirectoryProcessor processor = new ProductImagesDirectoryProcessor(mockFileSystem, productImageFileValidator);

            ProductImageDirectoryViewItem[] items = processor.Process(@"e:\temp\images");

            Assert.Empty(items);
        }

        [Fact]
        public void DirectoryWithEmptyDirectoyTest()
        {
            MockFileSystem mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"e:\temp\images\test");

            ProductImagesDirectoryProcessor processor = new ProductImagesDirectoryProcessor(mockFileSystem, productImageFileValidator);

            ProductImageDirectoryViewItem[] items = processor.Process(@"e:\temp\images");

            Assert.NotEmpty(items);

            ProductImageDirectoryViewItem item = items.First();

            Assert.False(item.Valid);
            Assert.Empty(item.Files);
            Assert.NotEmpty(item.GetValidationResults());
        }

        [Fact]
        public void ValidFileTest()
        {
            const string RootDirectoryPath = @"e:\temp\images";
            const string DirectoryName = "some_product_name";
            const int FileSize = 128;
            string[] files = { "1.jpg", "2.jpeg", "3.png" };

            Randomizer random = new Randomizer();

            Dictionary<string, MockFileData> mockFileDatas = files.ToDictionary(
                x => Path.Combine(RootDirectoryPath, DirectoryName, x),
                x => new MockFileData(random.Bytes(FileSize)));

            ProductImagesDirectoryProcessor processor = new ProductImagesDirectoryProcessor(new MockFileSystem(mockFileDatas), productImageFileValidator);

            ProductImageDirectoryViewItem[] items = processor.Process(RootDirectoryPath).ToArray();

            Assert.NotEmpty(items);

            ProductImageDirectoryViewItem item = items.First();

            Assert.True(item.Valid);
            Assert.Equal(RootDirectoryPath, item.Path);
            Assert.Equal(DirectoryName, item.DirectoryName);
            Assert.False(item.IsProcessed);
            Assert.Null(item.ProductId);
            Assert.Null(item.ProductName);

            Assert.NotNull(item.Files);
            Assert.Equal(files.Length, item.Files.Length);

            for (int i = 0; i < files.Length; i++)
            {
                ProductImageFile fileItem = item.Files[i];

                Assert.Equal(files[i], fileItem.FileName);
                Assert.Equal(FileSize, fileItem.FileSize);
            }
        }

        [Fact]
        public void InvalidFileTest()
        {
            const string RootDirectoryPath = @"e:\temp\images";
            const string DirectoryName = "some_product_name";
            const string FileName = "test.gif";
            const string ValidFileName = "test.JpG";
            int fileSize = (int)6.Megabytes().Bytes;

            Randomizer random = new Randomizer();

            IFileSystem mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [Path.Combine(RootDirectoryPath, DirectoryName, FileName)] = new MockFileData(random.Bytes(fileSize)),
                [Path.Combine(RootDirectoryPath, DirectoryName, ValidFileName)] = new MockFileData(random.Bytes(fileSize))
            });

            ProductImagesDirectoryProcessor processor = new ProductImagesDirectoryProcessor(mockFileSystem, productImageFileValidator);

            ProductImageDirectoryViewItem[] items = processor.Process(RootDirectoryPath).ToArray();

            Assert.NotEmpty(items);

            Assert.Single(items.First().Files);
        }
    }
}