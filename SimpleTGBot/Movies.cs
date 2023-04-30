using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SimpleTGBot.Movies;

namespace SimpleTGBot
{
    public class Movies
    {
        public List<Movie>? movies;
        public class Movie 
        {
            public string Title;
            public int Year;
            public string Country;
            public double VoteAverage;
            public string Description;
            public string Director;
            public string Screenwriter;
            public string Actors;
            public string Image;
            public Movie(string s)
            {
                var movie = s.Split(',');

                Title = movie[1];
                Year = int.Parse(movie[2]);
                Country = movie[3];
                VoteAverage = double.Parse(movie[4].Replace(".", ","));
                Description = movie[5];
                Director = movie[6];
                Screenwriter = movie[7];
                Actors = movie[8];
                Image = movie[9].Trim('\'');
            }
        }

        /// <summary>
        /// Преобразовывает файл в список фильмов.
        /// </summary>
        /// <param name="path"></param>
        public void FileConversion(string path)
        {
            movies = new List<Movie>();

            string[] lines = File.ReadAllLines(path);

            for (int i = 1; i < lines.Length; i++)
            {
                movies.Add(new Movie(lines[i]));
            }      
            
        }
       
        /// <summary>
        /// Возвращает рандомный фильм.
        /// </summary>
        /// <returns></returns>
        public Movie Random()
        {
            var rnd = new Random();
            var ind = rnd.Next(1, 250);
            return movies[ind];
        }

        /// <summary>
        /// Возвращет строку с 10 лучшими фильмами.
        /// </summary>
        /// <returns></returns>
        public string Top10()
        {
            var s = "";
            s += "Топ 10 фильмов: \n";
            for (int i = 0; i < 10; i++)
            {
                var m = movies[i];
                s += $"{i + 1}. {m.Title}. Рейтинг: {m.VoteAverage}. Режиссёр: {m.Director}\n";
            }
            return s;
        }

        /// <summary>
        /// Находит фильм по названию.
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public Movie SearchMovie(string title)
        {
            for (int i = 0; i < movies.Count; i++)
            {
                if (movies[i].Title.ToLower().Contains(title.ToLower()))
                {
                    return movies[i];
                }

            }
            return null;
        }
        /// <summary>
        /// Находит фильм по году.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="country"></param>
        /// <returns></returns>
        public Movie SearchMovieYear(int year) 
        { 
            var l = new List<Movie>();

            foreach(var m in movies)
            {
                if(m.Year == year)
                {              
                    l.Add(m);
                }                   
            }
            
            if( l.Count != 0)
            {
                var rnd = new Random();
                var ind = rnd.Next(0, l.Count);
                return l[ind];
            }

            return null;
        }

        /// <summary>
        /// Находит рандомный фильм по стране.
        /// </summary>
        /// <param name="country"></param>
        /// <returns></returns>
        public Movie SearchMovieCountry(string country)
        {
            var l = new List<Movie>();

            foreach (var m in movies)
            {
                if (m.Country.ToLower() == country.ToLower())
                {
                    l.Add(m);
                }
            }
            
            if(l.Count != 0)
            {
                var rnd = new Random();
                var ind = rnd.Next(0, l.Count);

                return l[ind];
            }

            return null;
            
        }

        /// <summary>
        /// Находит фильм по оценке.
        /// </summary>
        /// <param name="voteAverage"></param>
        /// <returns></returns>
        public Movie SearchMovieAverage(double voteAverage)
        {
            
            for(int i = movies.Count- 1; i >= 0;i--)
            {
                if (movies[i].VoteAverage >= voteAverage)
                {
                    return movies[i];
                }

            }
            return null;
        }

        /// <summary>
        /// Находит фильм по режиссеру
        /// </summary>
        /// <returns></returns>
        public Movie SearchMovieDirector(string director)
        {
            var l = new List<Movie>();
            foreach (var m in movies)
            {
                if (m.Director.ToLower().Contains(director.ToLower()))
                {
                    l.Add(m);
                }
            }

            if(l.Count != 0)
            {
                var rnd = new Random();
                var ind = rnd.Next(0, l.Count);

                return l[ind];
            }

            return null;           
        }

    }

}
