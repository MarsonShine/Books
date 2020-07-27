using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MomeryAllocation.ObjectCache
{
    public class SecondaryIndex
    {
        class Person
        {
            public string Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime Birthday { get; set; }
        }

        class PersonDatabase
        {
            private Dictionary<string, Person> index = new Dictionary<string, Person>();
            private Dictionary<DateTime, List<WeakReference<Person>>> birthdayIndex = new Dictionary<DateTime, List<WeakReference<Person>>>();

            public bool NeedsIndexRebuild { get; private set; }
            public void AddPerson(Person person)
            {
                this.index[person.Id] = person;
                if (!this.birthdayIndex.TryGetValue(person.Birthday, out List<WeakReference<Person>> birthdayList))
                {
                    birthdayIndex[person.Birthday] = birthdayList = new List<WeakReference<Person>>();
                }

                birthdayList.Add(new WeakReference<Person>(person));
            }

            public void RemovePerson(string id) => index.Remove(id);

            public bool TryGetById(string id, out Person person) => this.index.TryGetValue(id, out person);

            public bool TryGetByBirthday(DateTime birthday,out List<Person> people)
            {
                people = null;
                if(this.birthdayIndex.TryGetValue(birthday,out List<WeakReference<Person>> weakRef))
                {
                    var list = new List<Person>(weakRef.Count);
                    foreach (var wp in weakRef)
                    {
                        Person person;
                        if(wp.TryGetTarget(out person))
                        {
                            list.Add(person);
                        }
                        else
                        {
                            // 否则就获取了一个 null 引用
                            this.NeedsIndexRebuild = true;
                        }
                    }
                    if(list.Count > 0)
                    {
                        people = list;
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
