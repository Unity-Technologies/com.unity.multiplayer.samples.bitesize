using NUnit.Framework;

namespace Unity.Netcode.Samples.APIDiorama.Tests.Editor
{
    public class ProjectTests
    {
        [Test, Timeout(30000)]
        public void FindMissingReferencesInBuiltScenes_NoMissingReferencesFound()
        {
            MissingReferencesFinder.FindMissingReferencesInAllBuiltScenes();
        }

        [Test, Timeout(30000)]
        public void FindMissingReferencesInAssets_NoMissingReferencesFound()
        {
            MissingReferencesFinder.FindMissingReferencesInAssets();
        }
    }
}