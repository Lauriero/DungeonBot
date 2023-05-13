using System;
using System.Collections.Generic;

namespace DungeonDiscordBot.Utilities;

public static class CollectionExtensions
{
    public static void Shuffle<T>(this IList<T> list)  
    {  
        Random rng = new Random();
        
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }  
    }
}