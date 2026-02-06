namespace Yamigisa
{

    public interface ISavable
    {
        void Save(ref SaveGameData data);
        void Load(SaveGameData data);
    }
}