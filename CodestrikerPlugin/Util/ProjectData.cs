namespace CodestrikerPlugin.Util
{
    public class ProjectData
    {
        public ProjectData()
        {
        }

        public ProjectData(
            string name,
            PendingSetWrapper[] sets)
        {
            ProjectName = name;
            Shelvesets = sets;
        }

        public string ProjectName { get; set; }
        public PendingSetWrapper[] Shelvesets { get; }
    }
}