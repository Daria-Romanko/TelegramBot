using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SimpleTGBot.Movies;

namespace SimpleTGBot
{
    public class Movies
    {
        public List<Movie> movies;
        public class Movie 
        {
            public string Title;
            public int Year;
            public string Country;
            public double VoteAverage;
            public string Description;
            public string Image;
            public Movie(string s)
            {
                var movie = s.Split(',');

                Title = movie[1];
                Year = int.Parse(movie[2]);
                Country = movie[3];
                VoteAverage = double.Parse(movie[4].Replace(".", ","));
                Description = movie[5];
                //Image = movie[];
            }
        }

        //преобразование файла в список фильмов
        public void FileConversion(string path)
        {
            movies = new List<Movie>();

            string[] lines = File.ReadAllLines(path);

            for (int i = 1; i < lines.Length; i++)
            {
                movies.Add(new Movie(lines[i]));
            }      
            
        }
       
        public Movie Random()
        {
            var rnd = new Random();
            var ind = rnd.Next(1, 250);
            return movies[ind];
        }

    }

}
