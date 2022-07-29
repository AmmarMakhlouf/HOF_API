namespace HOF_API.Model
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }

        public bool Equals(Person obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Person p = (Person)obj;
                return (p.Id == this.Id) && (p.Name == this.Name) && (p.DisplayName == this.DisplayName);
            }
        }
    }
}
