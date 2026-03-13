using UnityEditor;

namespace DrscfZ.Editor
{
    public static class RunFixWorkerMesh
    {
        [MenuItem("Tools/DrscfZ/Run Fix Worker Mesh NOW")]
        public static void Execute()
        {
            FixWorkerMesh.Execute();
        }
    }
}
