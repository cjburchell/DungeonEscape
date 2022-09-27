
using System;
using System.Collections.Generic;
using System.Linq;
using Random = Nez.Random;

namespace Redpoint.DungeonEscape.Tools
{
  using State;
  
  public class NameGenerator
  {
    private readonly Names _data;

    public NameGenerator(Names data)
    {
      _data = data;
    }

    public string Generate(Gender type)
    {
      var chain = GetChain(type);
      if (chain != null)
      {
        return chain.GenerateName();
      }

      return "";
    }

    private class Chain
    {
      public string GenerateName()
      {
        var (parts, _) = select_link("parts");
        List<string> names = new();

        for (var i = 0; i < int.Parse(parts); i++)
        {
          var (nameLen, _) = select_link("name_len");
          var (c, _) = select_link("initial");
          var name = c;

          while (name.Length < int.Parse(nameLen))
          {
            var (newC, result) = select_link(c);
            if (!result)
            {
              break;
            }

            name += newC;
            c = newC;
          }

          names.Add(name);
        }

        return string.Join(' ', names);
      }

      private (string, bool) select_link (string key) {
        var len = _chain["table_len"][key];
        var idx = Math.Floor(Random.NextFloat() * len);
        var tokens = _chain[key].Keys;
        double acc = 0;
        foreach (var token in tokens)
        {
          acc += _chain[key][token];
          if (acc > idx) return (token, true);
        }
        
        return (null, false);
      }

      private void IncrementChain(string key, string token)
      {
        if (_chain.ContainsKey(key))
        {
          if (_chain[key].ContainsKey(token))
          {
            _chain[key][token]++;
          }
          else
          {
            _chain[key][token] = 1;
          }
        }
        else
        {
          _chain[key] = new Dictionary<string, double>
          {
            [token] = 1
          };
        }
      }

      readonly Dictionary<string, Dictionary<string, double>> _chain = new();

      public void Construct(List<string> data)
      {
        foreach (var names in data.Select(nameData => nameData.Split(" ")))
        {
          IncrementChain("parts", names.Length.ToString());

          foreach (var name in names)
          {
            IncrementChain("name_len", name.Length.ToString());

            var c = name.Substring(0, 1);
            IncrementChain("initial", c);

            var substr = name[1..];
            while (substr.Length > 0)
            {
              var c2 = substr.Substring(0, 1);
              IncrementChain(c, c2);

              substr = substr.Substring(1);
              c = c2;
            }
          }
        }

        scale_chain();
      }

      private void scale_chain()
      {
        var tableLen = new Dictionary<string, double>();
        foreach (var key in _chain.Keys.ToList())
        {
          tableLen[key] = 0;
          foreach (var token in _chain[key].Keys)
          {
            var count = _chain[key][token];
            var weighted = Math.Floor(Math.Pow(count, 1.3));

            _chain[key][token] = weighted;
            tableLen[key] += weighted;
          }

          _chain["table_len"] = tableLen;
        }
      }
    }

    private readonly Dictionary<Gender, Chain> _chainCache = new();
    private Chain GetChain(Gender type)
    {
      if (!this._chainCache.ContainsKey(type))
      {
        var chain = new Chain();
        chain.Construct(type == Gender.Male ? this._data.Male : this._data.Female);
        this._chainCache.Add(type, chain);
      }

      return this._chainCache[type];
    }
  }
}
