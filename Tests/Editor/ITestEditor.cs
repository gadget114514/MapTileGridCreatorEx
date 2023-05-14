using NUnit.Framework;

namespace MapTileGridCreator.Tests
{
	internal interface ITestEditor
	{
		[OneTimeSetUp]
		void Init();

		[SetUp]
		void BeforeTest();

		[TearDown]
		void AfterTest();

		[OneTimeTearDown]
		void End();
	}
}
