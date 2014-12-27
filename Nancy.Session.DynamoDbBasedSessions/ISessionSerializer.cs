namespace Nancy.Session
{
    public interface ISessionSerializer
    {
        ISession Deserialize(string data);
        string Serialize(ISession session);
    }
}