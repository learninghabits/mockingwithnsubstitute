using System.Data.Entity;

namespace mocking_with_nsubstitute.DB
{
    public interface ITopicsDataContext
    {
        IDbSet<Topic> Topics { get; set; }
        IDbSet<Tutorial> Tutorials { get; set; }
    }
}
