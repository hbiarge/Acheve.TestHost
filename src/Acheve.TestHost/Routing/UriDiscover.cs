﻿using Acheve.TestHost.Routing.AttributeTemplates;
using Acheve.TestHost.Routing.Tokenizers;
using System.Collections.Generic;
using System.Linq;

namespace Acheve.TestHost.Routing
{
    static class UriDiscover
    {
        static IEnumerable<ITokenizer> _tokenizers;

        static UriDiscover()
        {
            _tokenizers = new List<ITokenizer>()
            {
                new PrimitiveParameterActionTokenizer(),
                new ComplexParameterActionTokenizer(),
                new DefaultConventionalTokenizer()
            };
        }

        public static string Discover<TController>(TestServerAction action,object tokenValues)
            where TController:class
        {
            //at this moment only the first route is considered.. 

            var testServerTokens = AddTokens<TController>(action, tokenValues);

            var controllerTemplate = new AttributeControllerRouteTemplates()
                .GetTemplates<TController>(action, testServerTokens)
                .FirstOrDefault();

            var verbsTemplate = new AttributeVerbsTemplate()
                .GetTemplates<TController>(action, testServerTokens)
                .FirstOrDefault();

            var routeTemplate = new AttributeRouteTemplates()
              .GetTemplates<TController>(action, testServerTokens)
              .FirstOrDefault();

            var queryStringTemplate = new QueryStringTemplate()
                .GetTemplates<TController>(action, testServerTokens)
                .FirstOrDefault();

            var template = (verbsTemplate ?? routeTemplate);

            if (template != null)
            {
                if (IsTildeOverride(template, out string overrideTemplate))
                {
                    return $"{overrideTemplate}{queryStringTemplate}";
                }
                else
                {
                    return $"{controllerTemplate}/{template}{queryStringTemplate}";
                }
            }

            return $"{controllerTemplate}{queryStringTemplate}";
        }

        static TestServerTokenCollection AddTokens<TController>(TestServerAction action, object tokenValues)
            where TController : class
        {
            var dictionaryTokenValues = new Dictionary<string, string>();

            if (tokenValues != null)
            {
                dictionaryTokenValues = tokenValues.GetType()
                    .GetProperties()
                    .ToDictionary(p => p.Name.ToLowerInvariant(), p => p.GetValue(tokenValues).ToString());
            }

            var testServerTokens = TestServerTokenCollection.FromDictionary(dictionaryTokenValues);

            foreach (var tokeniker in _tokenizers)
            {
                tokeniker.AddTokens<TController>(action, testServerTokens);
            }

            return testServerTokens;
        }
        static bool IsTildeOverride(string template, out string overrideTemplate)
        {
            const string TILDE = "~";

            overrideTemplate = null;
            var isTildeOverride = template.StartsWith(TILDE);

            if (isTildeOverride)
            {
                overrideTemplate = template.Substring(2); // remove ~/
            }

            return isTildeOverride;
        }
    }
}
