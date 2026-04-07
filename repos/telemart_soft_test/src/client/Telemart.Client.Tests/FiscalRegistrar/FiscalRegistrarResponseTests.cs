using System.Linq;
using Telemart.Client.FiscalRegistrar.Responses.Base;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests.FiscalRegistrar
{
    public sealed class FiscalRegistrarResponseTests
    {
        [Fact]
        public void OkResponseWithoutErrorTest()
        {
            string json = @"{
    'model': 'MG N707TS',
    'name': 'IC30801621',
    'serial': 'IC30801621'
}";
            TestResponse response = new TestResponse(json);

            Assert.True(response.IsOk);
            Assert.False(response.IsError);
            Assert.Empty(response.GetErrorMessages());
        }

        [Fact]
        public void OkResponseWithEmptyArrayErrorTest()
        {
            string json = @"{
    'model': 'MG N707TS',
    'name': 'IC30801621',
    'serial': 'IC30801621',
    'err': []
}";
            TestResponse response = new TestResponse(json);

            Assert.True(response.IsOk);
            Assert.False(response.IsError);
            Assert.Empty(response.GetErrorMessages());
        }

        [Fact]
        public void OkResponseWithNullErrorTest()
        {
            string json = @"{
    'model': 'MG N707TS',
    'name': 'IC30801621',
    'serial': 'IC30801621',
    'err': null
}";
            TestResponse response = new TestResponse(json);

            Assert.True(response.IsOk);
            Assert.False(response.IsError);
            Assert.Empty(response.GetErrorMessages());
        }

        [Fact]
        public void OkResponseWithEmtptyStringErrorTest()
        {
            string json = @"{
    'model': 'MG N707TS',
    'name': 'IC30801621',
    'serial': 'IC30801621',
    'err': ''
}";
            TestResponse response = new TestResponse(json);

            Assert.True(response.IsOk);
            Assert.False(response.IsError);
            Assert.Empty(response.GetErrorMessages());
        }

        [Theory]
        [InlineData("Ошибка", "Ошибка")]
        [InlineData("Переполнение ленты", "xFF")]
        public void ErrorResponseWithStringErrorTest(string expected, string error)
        {
            string json = $@"{{
    'model': 'MG N707TS',
    'name': 'IC30801621',
    'serial': 'IC30801621',
    'err': '{error}'
}}";
            TestResponse response = new TestResponse(json);

            Assert.False(response.IsOk);
            Assert.True(response.IsError);
            Assert.NotEmpty(response.GetErrorMessages());

            string errorString = response.GetErrorMessages().First();

            Assert.Equal(expected, errorString);
        }

        [Fact]
        public void ErrorResponseWithObjectErrorTest()
        {
            string json = @"{
    'model': 'MG N707TS',
    'name': 'IC30801621',
    'serial': 'IC30801621',
    'err': { 'e': 'xFF' }
}";
            TestResponse response = new TestResponse(json);

            Assert.False(response.IsOk);
            Assert.True(response.IsError);
            Assert.NotEmpty(response.GetErrorMessages());

            string errorString = response.GetErrorMessages().First();

            Assert.Equal("Переполнение ленты", errorString);
        }

        [Fact]
        public void ErrorResponseWithArrayErrorTest()
        {
            string json = @"{
    'model': 'MG N707TS',
    'name': 'IC30801621',
    'serial': 'IC30801621',
    'err': [ { 'e': 'xFF' }, { 'e': 'xFA' } ]
}";
            TestResponse response = new TestResponse(json);

            Assert.False(response.IsOk);
            Assert.True(response.IsError);
            Assert.NotEmpty(response.GetErrorMessages());
            Assert.Collection(
                response.GetErrorMessages(),
                x => Assert.Equal("Переполнение ленты", x),
                x => Assert.Equal("У кассира нет прав на эту операцию", x));
        }

        private class TestResponse : FiscalRegistrarResponseBase
        {
            public TestResponse(string raw)
                : base(raw)
            {
            }

            public TestResponse(int statusCode, string reason)
                : base(statusCode, reason)
            {
            }
        }
    }
}