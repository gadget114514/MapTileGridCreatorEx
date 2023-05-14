using NUnit.Framework;

using UnityEngine.TestTools;

namespace MapTileGridCreator.Tests
{
	internal interface ITestUnity
	{
		[OneTimeSetUp]
		void Init();

		[UnitySetUp]
		void BeforeTest();

		[UnityTearDown]
		void AfterTest();

		[OneTimeTearDown]
		void End();
	}
}
