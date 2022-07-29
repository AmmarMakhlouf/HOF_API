namespace HOF_API.Model
{
    public class PersonSkill
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public Person Person { get; set; }


        public int SkillId { get; set; }
        public Skill Skill { get; set; }
        public byte Level { get; set; }


    }
}
