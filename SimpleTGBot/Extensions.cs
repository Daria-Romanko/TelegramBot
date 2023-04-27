using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTGBot
{
    internal class Extensions
    {
        public static string Top10(List<Movies.Movie> movies)
        {
            var s = "";
            s += "Топ 10 фильмов: \n";
            for(int i = 0; i < 10; i++)
            {
                var m = movies[i];
                s += $"{i+1}. {m.Title}. Рейтинг: {m.VoteAverage}. Режиссёр: {m.Director}\n";
            }
            return s;
        }
    }
}
