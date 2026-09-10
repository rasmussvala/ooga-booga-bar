using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace OogaBoogaBar.Tests
{
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator PlayModeTestsRun()
        {
            var go = new GameObject("probe");
            yield return null;
            Assert.IsNotNull(go);
            Object.DestroyImmediate(go);
        }
    }
}
