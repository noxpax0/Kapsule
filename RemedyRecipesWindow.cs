using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FuturisticCtrlHud;

public sealed class RemedyRecipesWindow : Window
{
    private const int MaxResults = 3;
    private const string Disclaimer = "For minor ailments only. Not medical advice. Seek professional help for serious, persistent, worsening, eye-related, allergic, pregnancy, child, or medication-interaction concerns.";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(14) };

    private readonly AppSettings _settings;
    private readonly TextBox _search = new();
    private readonly TextBlock _status = new();
    private readonly StackPanel _results = new();
    private readonly Button _searchButton = new();

    private readonly TextBox _customName = new();
    private readonly TextBox _customPurpose = new();
    private readonly TextBox _customSafety = MultilineBox(60);
    private readonly TextBox _customQuantity = NumericBox("1");
    private readonly TextBox _customPeople = NumericBox("1");
    private readonly StackPanel _customIngredients = new();
    private readonly TextBox _customSteps = MultilineBox(90);
    private readonly TextBox _customSources = MultilineBox(60);
    private readonly List<IngredientEditorRow> _customRows = [];

    public RemedyRecipesWindow(AppSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();

        Title = "Safe Home Remedy Recipes";
        Width = 980;
        Height = 760;
        MinWidth = 820;
        MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent();
    }

    private UIElement BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(18) };
        var bottom = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        bottom.Children.Add(Button("Close", "#F6F8FA", (_, _) => Close(), 96));
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var content = new StackPanel();
        scroll.Content = content;
        root.Children.Add(scroll);

        content.Children.Add(new TextBlock
        {
            Text = "Safe Home Remedy Recipes",
            FontSize = 25,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        content.Children.Add(Notice(Disclaimer));
        content.Children.Add(BuildSearchPanel());
        content.Children.Add(BuildCustomPanel());
        content.Children.Add(_status);
        content.Children.Add(_results);

        RenderWelcome();
        return root;
    }

    private UIElement BuildSearchPanel()
    {
        var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 14) };
        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(128) });

        _search.Height = 54;
        _search.FontSize = 23;
        _search.FontWeight = FontWeights.SemiBold;
        _search.Padding = new Thickness(12, 5, 12, 5);
        _search.ToolTip = "Find safe home remedy";
        _search.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                await SearchAsync();
            }
        };
        row.Children.Add(_search);

        _searchButton.Content = "Search";
        _searchButton.Width = 112;
        _searchButton.Height = 54;
        _searchButton.Margin = new Thickness(12, 0, 0, 0);
        _searchButton.FontWeight = FontWeights.SemiBold;
        _searchButton.Background = Brush("#E6FFFA");
        _searchButton.BorderBrush = Brush("#99F6E4");
        _searchButton.Click += async (_, _) => await SearchAsync();
        Grid.SetColumn(_searchButton, 1);
        row.Children.Add(_searchButton);
        panel.Children.Add(row);

        panel.Children.Add(new TextBlock
        {
            Text = "Uses built-in safe recipes first, then optional OpenAI-compatible API, then public repository lookups such as Wikipedia, PubMed, OpenFDA, and DuckDuckGo where useful.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.72
        });
        panel.Children.Add(BuildQuickSearches());
        return panel;
    }

    private UIElement BuildQuickSearches()
    {
        var wrap = new WrapPanel { Margin = new Thickness(0, 9, 0, 0) };
        foreach (var quick in QuickSearches)
        {
            var button = new Button
            {
                Content = quick.Label,
                Tag = quick.Query,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 2, 10, 2),
                Background = Brush(quick.Color),
                BorderBrush = Brushes.LightGray,
                FontWeight = FontWeights.SemiBold
            };
            button.Click += async (_, _) =>
            {
                _search.Text = quick.Query;
                await SearchAsync();
            };
            wrap.Children.Add(button);
        }

        return wrap;
    }

    private UIElement BuildCustomPanel()
    {
        var expander = new Expander
        {
            Header = "Create Custom Remedy Recipe",
            Margin = new Thickness(0, 0, 0, 16),
            FontWeight = FontWeights.SemiBold
        };

        var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        panel.Children.Add(Field("Remedy name", _customName));
        panel.Children.Add(Field("Minor ailment / purpose", _customPurpose));
        panel.Children.Add(Field("Safety notes", _customSafety));

        var qty = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        qty.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        qty.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        qty.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        qty.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        qty.Children.Add(new TextBlock { Text = "Final Quantity Required", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(_customQuantity, 1);
        qty.Children.Add(_customQuantity);
        qty.Children.Add(new TextBlock { Text = "Number of People", Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(qty.Children[^1], 2);
        Grid.SetColumn(_customPeople, 3);
        qty.Children.Add(_customPeople);
        panel.Children.Add(qty);

        panel.Children.Add(new TextBlock { Text = "Ingredients", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 4) });
        panel.Children.Add(BuildIngredientHeader(showRemove: true));
        panel.Children.Add(_customIngredients);
        AddCustomIngredient();

        var ingredientButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        ingredientButtons.Children.Add(Button("Add ingredient", "#ECFDF5", (_, _) => AddCustomIngredient(), 132));
        panel.Children.Add(ingredientButtons);

        panel.Children.Add(Field("Preparation steps", _customSteps));
        panel.Children.Add(Field("Source/reference notes", _customSources));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(Button("Save Custom Card", "#FEF3C7", (_, _) => SaveCustomRecipe(), 155));
        actions.Children.Add(Button("Print Custom Recipe", "#E8F1FF", (_, _) => PrintRecipe(BuildCustomRecipe()), 170));
        panel.Children.Add(actions);

        expander.Content = panel;
        return expander;
    }

    private async Task SearchAsync()
    {
        var query = _search.Text.Trim();
        if (query.Length == 0)
        {
            RenderWelcome();
            return;
        }

        query = query.Length > 120 ? query[..120] : query;
        _searchButton.IsEnabled = false;
        _status.Text = "Searching safe recipe matches...";
        _results.Children.Clear();

        try
        {
            if (IsUnsafe(query, out var reason))
            {
                _status.Text = reason;
                return;
            }

            var searchTerms = ExpandQuery(query);
            var local = MergeResults(CustomSearch(searchTerms), OfflineSearch(searchTerms));
            var api = await TryOpenAiCompatibleSearchAsync(string.Join("; ", searchTerms.Take(5)));
            var repository = await TryRepositorySearchAsync(query, searchTerms);
            var localForTop = repository.Count > 0 && api is null
                ? local.Take(Math.Max(1, MaxResults - 1)).ToList()
                : local;
            var results = MergeResults(api, localForTop, repository, local).Take(MaxResults).ToList();

            if (results.Count == 0)
            {
                _status.Text = $"No safe recipe match found for {string.Join(", ", searchTerms.Take(4))}. Try a minor, low-risk phrase such as salt bath, saline rinse, foot deodorant, oatmeal soak, or honey lemon comfort drink.";
                return;
            }

            _status.Text = api is not null
                ? $"Showing top safe matches for: {string.Join(", ", searchTerms.Take(4))}. Configured API plus local/public safety filters were used."
                : repository.Count > 0
                    ? $"Showing top safe matches for: {string.Join(", ", searchTerms.Take(4))}. Public repository lookups were included."
                    : $"Showing built-in safe matches for: {string.Join(", ", searchTerms.Take(4))}. Public lookups found no extra concise match.";
            RenderResults(results);
        }
        catch (Exception ex)
        {
            _status.Text = $"Recipe search failed: {ex.Message}";
        }
        finally
        {
            _searchButton.IsEnabled = true;
        }
    }

    private async Task<List<RemedyRecipe>?> TryOpenAiCompatibleSearchAsync(string query)
    {
        var endpoint = _settings.Presets.RemedyApiEndpoint;
        var model = _settings.Presets.RemedyApiModel;
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrWhiteSpace(_settings.Presets.RemedyApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Presets.RemedyApiKey);
            }

            var body = new
            {
                model,
                temperature = 0.15,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = $"Find up to three safe minor-ailment home remedy recipes for: {query}" }
                }
            };
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var content = JsonNode.Parse(json)?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<RemedyRecipe>>(content, JsonOptions)?
                .Where(IsSafeApiRecipe)
                .Select(recipe =>
                {
                    recipe.SourceKind = RecipeSourceKind.Online;
                    return recipe;
                })
                .Take(MaxResults)
                .ToList();
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<RemedyRecipe>> TryRepositorySearchAsync(string query, IReadOnlyList<string> searchTerms)
    {
        var recipes = new List<RemedyRecipe>();

        foreach (var term in searchTerms.Take(4))
        {
            var encoded = Uri.EscapeDataString(term);

            try
            {
                var ddgUrl = $"https://api.duckduckgo.com/?q={encoded}+safe+home+remedy&format=json&no_redirect=1&no_html=1";
                var ddg = JsonNode.Parse(await Http.GetStringAsync(ddgUrl));
                var heading = ddg?["Heading"]?.GetValue<string>() ?? "";
                var abstractText = ddg?["AbstractText"]?.GetValue<string>() ?? "";
                var abstractUrl = ddg?["AbstractURL"]?.GetValue<string>() ?? "";
                if (!string.IsNullOrWhiteSpace(abstractText) && !ContainsAny(abstractText, HardBlockedTerms))
                {
                    recipes.Add(RepositoryRecipe(
                        string.IsNullOrWhiteSpace(heading) ? $"Safe recipe idea for {term}" : heading,
                        query,
                        term,
                        abstractText,
                        abstractUrl));
                }
            }
            catch
            {
                // Public search is best-effort; local recipes remain available.
            }
        }

        foreach (var term in searchTerms.Take(5))
        {
            try
            {
                foreach (var recipe in await TryWikipediaRecipesAsync(query, term))
                {
                    recipes.Add(recipe);
                }
            }
            catch
            {
                // Wikipedia lookup is optional.
            }
        }

        var safetyLinks = await TrySafetyReferenceLinksAsync(searchTerms);
        foreach (var recipe in recipes)
        {
            foreach (var link in safetyLinks)
            {
                if (!recipe.SourceLinks.Contains(link, StringComparer.OrdinalIgnoreCase))
                {
                    recipe.SourceLinks.Add(link);
                }
            }
        }

        return recipes.Take(MaxResults).ToList();
    }

    private static async Task<List<RemedyRecipe>> TryWikipediaRecipesAsync(string originalQuery, string term)
    {
        var recipes = new List<RemedyRecipe>();
        var openSearchUrl = $"https://en.wikipedia.org/w/api.php?action=opensearch&search={Uri.EscapeDataString(term)}&limit=2&namespace=0&format=json";
        var openSearch = JsonNode.Parse(await Http.GetStringAsync(openSearchUrl))?.AsArray();
        var titles = openSearch?.Count > 1
            ? openSearch[1]?.AsArray().Select(node => node?.GetValue<string>() ?? "").Where(title => title.Length > 0).ToList() ?? []
            : [];

        if (titles.Count == 0)
        {
            titles.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(term.ToLowerInvariant()));
        }

        foreach (var title in titles.Take(2))
        {
            var wikiUrl = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(title)}";
            var wiki = JsonNode.Parse(await Http.GetStringAsync(wikiUrl));
            var extract = wiki?["extract"]?.GetValue<string>() ?? "";
            var url = wiki?["content_urls"]?["desktop"]?["page"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrWhiteSpace(extract) && !ContainsAny(extract, HardBlockedTerms))
            {
                recipes.Add(RepositoryRecipe($"{title} source recipe card", originalQuery, term, extract, url));
            }
        }

        return recipes;
    }

    private static async Task<List<string>> TrySafetyReferenceLinksAsync(IReadOnlyList<string> searchTerms)
    {
        var links = new List<string>();
        var safetyTerm = searchTerms.FirstOrDefault(term => term.Length > 0) ?? "";
        var encoded = Uri.EscapeDataString($"{safetyTerm} safety");

        try
        {
            var pubMedUrl = $"https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed&term={encoded}&retmode=json&retmax=1";
            var pubMed = JsonNode.Parse(await Http.GetStringAsync(pubMedUrl));
            var id = pubMed?["esearchresult"]?["idlist"]?[0]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(id))
            {
                links.Add($"https://pubmed.ncbi.nlm.nih.gov/{id}/");
            }
        }
        catch
        {
            // PubMed references are best-effort.
        }

        try
        {
            var substance = Uri.EscapeDataString(safetyTerm.Replace("\"", "", StringComparison.Ordinal).Trim());
            var openFdaUrl = $"https://api.fda.gov/drug/label.json?search=openfda.substance_name:%22{substance}%22&limit=1";
            var openFda = JsonNode.Parse(await Http.GetStringAsync(openFdaUrl));
            if (openFda?["results"]?[0] is not null)
            {
                links.Add($"https://api.fda.gov/drug/label.json?search=openfda.substance_name:%22{substance}%22&limit=1");
            }
        }
        catch
        {
            // OpenFDA has uneven coverage for household ingredients.
        }

        return links;
    }

    private static RemedyRecipe RepositoryRecipe(string title, string query, string matchedTerm, string note, string url) => new()
    {
        Title = title.Length > 70 ? $"{title[..67]}..." : title,
        Purpose = $"Repository background for: {query}. Matched concept: {matchedTerm}.",
        SourceKind = RecipeSourceKind.Online,
        SafetyNote = "This is a source-backed background card, not a validated recipe. Use only simple low-risk ingredients and do not replace professional care.",
        FinalQuantity = 1,
        People = 1,
        Ingredients =
        [
            new RemedyIngredient { Name = "Source review", Notes = "Public repository result; use as background before creating a custom safe recipe." }
        ],
        Steps =
        [
            "Read the source note.",
            "If appropriate, create a custom recipe using only simple, familiar, low-risk household ingredients.",
            "Do not use if the concern is serious, persistent, worsening, eye-related, allergic, pregnancy-related, child-related, or medication-related."
        ],
        SourceNotes = [Shorten(note, 220)],
        SourceLinks = string.IsNullOrWhiteSpace(url) ? [] : [url]
    };

    private static List<RemedyRecipe> MergeResults(params List<RemedyRecipe>?[] lists)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<RemedyRecipe>();
        foreach (var list in lists)
        {
            if (list is null)
            {
                continue;
            }

            foreach (var recipe in list)
            {
                if (seen.Add(recipe.Title))
                {
                    merged.Add(recipe);
                }
            }
        }

        return merged;
    }

    private static bool IsSafeApiRecipe(RemedyRecipe recipe)
    {
        var allText = string.Join(" ", recipe.Title, recipe.Purpose, recipe.SafetyNote,
            string.Join(" ", recipe.Ingredients.Select(i => $"{i.Name} {i.Notes}")),
            string.Join(" ", recipe.Steps),
            string.Join(" ", recipe.SourceNotes));

        return recipe.Title.Trim().Length > 0
            && recipe.Ingredients.Count > 0
            && recipe.Steps.Count > 0
            && !ContainsAny(allText, HardBlockedTerms);
    }

    private static List<RemedyRecipe> OfflineSearch(IReadOnlyList<string> searchTerms)
    {
        var tokens = searchTerms.SelectMany(Tokens).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return OfflineRecipes
            .Select(recipe => new { Recipe = recipe.Clone(), Score = Score(recipe, tokens) })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Recipe.Title)
            .Take(MaxResults)
            .Select(match => match.Recipe)
            .ToList();
    }

    private List<RemedyRecipe> CustomSearch(IReadOnlyList<string> searchTerms)
    {
        var tokens = searchTerms.SelectMany(Tokens).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return _settings.Presets.CustomRemedyRecipes
            .Select(recipe => new { Recipe = recipe.Clone(), Score = Score(recipe, tokens) })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Recipe.Title)
            .Take(MaxResults)
            .Select(match =>
            {
                match.Recipe.IsCustom = true;
                match.Recipe.SourceKind = RecipeSourceKind.Custom;
                return match.Recipe;
            })
            .ToList();
    }

    private static int Score(RemedyRecipe recipe, string[] tokens)
    {
        var haystack = string.Join(" ", recipe.Title, recipe.Purpose, recipe.SafetyNote,
            string.Join(" ", recipe.Ingredients.Select(i => i.Name)),
            string.Join(" ", recipe.SearchAliases)).ToUpperInvariant();
        var haystackTokens = Tokens(haystack);
        var score = 0;
        foreach (var token in tokens)
        {
            var index = haystack.IndexOf(token, StringComparison.Ordinal);
            if (index >= 0)
            {
                score += index < recipe.Title.Length ? 22 : 9;
                continue;
            }

            if (haystackTokens.Any(candidate => IsCloseToken(token, candidate)))
            {
                score += 6;
            }
        }

        return score;
    }

    private static string[] Tokens(string query) =>
        query.ToUpperInvariant()
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeToken)
            .Where(token => token.Length > 2)
            .ToArray();

    private static List<string> ExpandQuery(string query)
    {
        var corrected = CorrectLikelyTypos(query.Trim());
        var terms = new List<string> { query.Trim(), corrected };
        foreach (var group in SearchConceptGroups)
        {
            if (group.Any(term => PhraseMatches(corrected, term)))
            {
                terms.AddRange(group);
            }
        }

        var normalized = corrected
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Trim();
        if (!terms.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            terms.Add(normalized);
        }

        return terms
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Select(term => term.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    private static string CorrectLikelyTypos(string query)
    {
        var words = query
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var corrected = words.Select(word =>
        {
            var normalized = NormalizeToken(word.ToUpperInvariant());
            var exact = SearchVocabulary.FirstOrDefault(candidate => candidate.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact.ToLowerInvariant();
            }

            var close = SearchVocabulary
                .Where(candidate => IsCloseToken(normalized, candidate))
                .OrderBy(candidate => Math.Abs(candidate.Length - normalized.Length))
                .FirstOrDefault();
            return close?.ToLowerInvariant() ?? word;
        });

        return string.Join(' ', corrected);
    }

    private static bool PhraseMatches(string query, string phrase)
    {
        if (query.Contains(phrase, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var queryTokens = Tokens(query);
        var phraseTokens = Tokens(phrase);
        return phraseTokens.Length > 0 && phraseTokens.All(phraseToken =>
            queryTokens.Any(queryToken => queryToken.Equals(phraseToken, StringComparison.OrdinalIgnoreCase) || IsCloseToken(queryToken, phraseToken)));
    }

    private static string NormalizeToken(string token)
    {
        var builder = new StringBuilder(token.Length);
        var previous = '\0';
        foreach (var raw in token)
        {
            if (!char.IsLetterOrDigit(raw))
            {
                continue;
            }

            var current = char.ToUpperInvariant(raw);
            if (current == previous)
            {
                continue;
            }

            builder.Append(current);
            previous = current;
        }

        return builder.ToString();
    }

    private static bool IsCloseToken(string left, string right)
    {
        if (left.Length < 4 || right.Length < 4)
        {
            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        if (left.StartsWith(right, StringComparison.OrdinalIgnoreCase) || right.StartsWith(left, StringComparison.OrdinalIgnoreCase))
        {
            return Math.Abs(left.Length - right.Length) <= 2;
        }

        var distance = EditDistance(left, right, maxDistance: 2);
        return distance <= (Math.Min(left.Length, right.Length) <= 5 ? 1 : 2);
    }

    private static int EditDistance(string left, string right, int maxDistance)
    {
        if (Math.Abs(left.Length - right.Length) > maxDistance)
        {
            return maxDistance + 1;
        }

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            var rowMin = current[0];
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                rowMin = Math.Min(rowMin, current[j]);
            }

            if (rowMin > maxDistance)
            {
                return maxDistance + 1;
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static bool IsUnsafe(string query, out string reason)
    {
        if (ContainsAny(query, PersonalInfoTerms))
        {
            reason = "This feature does not collect or process personal information. Search only for a general remedy recipe.";
            return true;
        }

        if (ContainsAny(query, EyeTerms))
        {
            reason = "Eye-related remedies are blocked. For eye wash, use only sterile saline or seek professional care.";
            return true;
        }

        if (ContainsAny(query, HardBlockedTerms))
        {
            reason = "Blocked for safety. This feature cannot provide remedies for serious illness, prescribed-medicine replacement, unsafe chemicals, essential-oil ingestion, ears/eyes, wounds, burns, infection, pregnancy, children, pets, allergic reactions, or medication-interaction concerns.";
            return true;
        }

        reason = "";
        return false;
    }

    private static bool ContainsAny(string text, IEnumerable<string> terms)
    {
        foreach (var term in terms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void RenderWelcome()
    {
        _status.Text = "Recipe cards start closed. Click a title card to open quantities, ingredients, notes, and print.";
        _results.Children.Clear();
        var custom = _settings.Presets.CustomRemedyRecipes.Select(recipe =>
        {
            var clone = recipe.Clone();
            clone.IsCustom = true;
            clone.SourceKind = RecipeSourceKind.Custom;
            return clone;
        });
        RenderResults(custom.Concat(OfflineRecipes.Take(3).Select(recipe => recipe.Clone())).ToList());
    }

    private void RenderResults(IReadOnlyList<RemedyRecipe> recipes)
    {
        _results.Children.Clear();
        foreach (var recipe in recipes)
        {
            _results.Children.Add(RecipeCard(recipe));
        }
    }

    private UIElement RecipeCard(RemedyRecipe recipe)
    {
        var ingredientRows = recipe.Ingredients.Select(ingredient => new IngredientEditorRow(ingredient.Clone(), allowRemove: false)).ToList();
        var quantity = NumericBox(recipe.FinalQuantity.ToString("0.##", CultureInfo.InvariantCulture));
        var people = NumericBox(recipe.People.ToString(CultureInfo.InvariantCulture));
        var ingredientsPanel = new StackPanel();

        void RefreshScale()
        {
            var qtyScale = ParseDouble(quantity.Text, recipe.FinalQuantity) / Math.Max(recipe.FinalQuantity, 0.01);
            var peopleScale = ParseDouble(people.Text, recipe.People) / Math.Max(recipe.People, 1);
            var scale = Math.Max(0.01, qtyScale * peopleScale);
            foreach (var row in ingredientRows)
            {
                row.ApplyScale(scale);
            }
        }

        foreach (var row in ingredientRows)
        {
            ingredientsPanel.Children.Add(row.Root);
        }

        quantity.TextChanged += (_, _) => RefreshScale();
        people.TextChanged += (_, _) => RefreshScale();

        var outer = new Border
        {
            Background = Brushes.Transparent,
            Margin = new Thickness(0, 0, 0, 12)
        };
        var expander = new Expander
        {
            IsExpanded = false,
            Background = Brushes.Transparent,
            Padding = new Thickness(0),
            Header = CardHeader(recipe)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var left = new Border
        {
            Background = Brush(recipe.IsCustom ? "#FDE68A" : "#E0F2FE"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 14, 0),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = recipe.IsCustom ? "CUSTOM" : "SAFE", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brush(recipe.IsCustom ? "#92400E" : "#0369A1") },
                    new TextBlock { Text = recipe.Title, FontSize = 21, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 8) },
                    new TextBlock { Text = recipe.Purpose, TextWrapping = TextWrapping.Wrap, Opacity = 0.8 }
                }
            }
        };
        Grid.SetColumn(left, 0);
        Grid.SetRowSpan(left, 4);
        grid.Children.Add(left);

        var top = PanelBox("#FFFFFF");
        top.Children.Add(new TextBlock { Text = recipe.SafetyNote, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Foreground = Brush("#9A3412") });
        top.Children.Add(BuildQuantityRow(quantity, people));
        Grid.SetColumn(top, 1);
        Grid.SetRow(top, 0);
        grid.Children.Add(top);

        var ingredients = PanelBox("#F0FDFA");
        ingredients.Children.Add(new TextBlock { Text = "Ingredients", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        ingredients.Children.Add(BuildIngredientHeader(showRemove: false));
        ingredients.Children.Add(ingredientsPanel);
        Grid.SetColumn(ingredients, 1);
        Grid.SetRow(ingredients, 1);
        grid.Children.Add(ingredients);

        var steps = PanelBox("#FFF7ED");
        steps.Children.Add(new TextBlock { Text = "Instructions", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            steps.Children.Add(new TextBlock { Text = $"{i + 1}. {recipe.Steps[i]}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) });
        }
        Grid.SetColumn(steps, 1);
        Grid.SetRow(steps, 2);
        grid.Children.Add(steps);

        var sources = PanelBox("#F9FAFB");
        sources.Children.Add(new TextBlock { Text = "Ingredient / source notes", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        foreach (var note in recipe.SourceNotes)
        {
            sources.Children.Add(new TextBlock { Text = $"- {note}", TextWrapping = TextWrapping.Wrap, Opacity = 0.85 });
        }

        foreach (var link in recipe.SourceLinks)
        {
            sources.Children.Add(new TextBlock { Text = link, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#0369A1") });
        }

        var printRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        printRow.Children.Add(Button("Print", "#E8F1FF", (_, _) => PrintRecipe(ReadRecipeFromCard(recipe, quantity, people, ingredientRows)), 88));
        sources.Children.Add(printRow);
        Grid.SetColumn(sources, 1);
        Grid.SetRow(sources, 3);
        grid.Children.Add(sources);

        var details = new Border
        {
            Padding = new Thickness(14),
            Child = grid
        };
        expander.Content = details;
        outer.Child = CardShell(recipe, expander);
        RefreshScale();
        return outer;
    }

    private static UIElement CardHeader(RemedyRecipe recipe)
    {
        var palette = CardPalette(recipe);
        var grid = new Grid { Margin = new Thickness(12, 10, 12, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(new TextBlock
        {
            Text = recipe.Title,
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush(palette.Text),
            VerticalAlignment = VerticalAlignment.Center
        });
        var source = SourceBadge(recipe);
        var pill = new Border
        {
            Background = Brush("#FFFFFF"),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(12, 0, 0, 0),
            Opacity = 0.94,
            Child = new TextBlock
            {
                Text = source.Label,
                Foreground = Brush(source.Color),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12
            }
        };
        Grid.SetColumn(pill, 1);
        grid.Children.Add(pill);
        var openHint = new TextBlock
        {
            Text = "Open",
            Foreground = Brush(palette.Text),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Opacity = 0.72,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(openHint, 2);
        grid.Children.Add(openHint);
        return grid;
    }

    private static UIElement CardShell(RemedyRecipe recipe, UIElement content)
    {
        var palette = CardPalette(recipe);
        var shell = new Grid { Margin = new Thickness(4, 0, 8, 0) };
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });

        for (var i = 3; i >= 1; i--)
        {
            var layer = new Border
            {
                Height = 18,
                Background = Brush(palette.Layer),
                BorderBrush = Brush(palette.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 0, 12, 12),
                Margin = new Thickness(12 + (i * 4), 0, 12 - (i * 2), 1 + (i * 3)),
                VerticalAlignment = VerticalAlignment.Bottom,
                Opacity = 0.82
            };
            Grid.SetRow(layer, 1);
            shell.Children.Add(layer);
        }

        var card = new Border
        {
            Background = Brush(palette.Main),
            BorderBrush = Brush(palette.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            MinHeight = 68,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(0x1F, 0x29, 0x37),
                BlurRadius = 12,
                ShadowDepth = 3,
                Opacity = 0.24
            },
            Child = new Grid
            {
                Children =
                {
                    new Border
                    {
                        Height = 18,
                        CornerRadius = new CornerRadius(14, 14, 0, 0),
                        Background = new LinearGradientBrush(
                            Color.FromArgb(120, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255),
                            new Point(0, 0),
                            new Point(1, 1)),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    content
                }
            }
        };
        Grid.SetRow(card, 0);
        Grid.SetRowSpan(card, 2);
        shell.Children.Add(card);
        return shell;
    }

    private static (string Main, string Layer, string Border, string Text) CardPalette(RemedyRecipe recipe)
    {
        if (recipe.IsCustom || recipe.SourceKind == RecipeSourceKind.Custom)
        {
            return ("#FBBF24", "#D97706", "#B45309", "#3F2500");
        }

        return recipe.SourceKind == RecipeSourceKind.Online
            ? ("#22C55E", "#15803D", "#166534", "#052E16")
            : ("#38BDF8", "#0284C7", "#0369A1", "#082F49");
    }

    private static (string Label, string Color) SourceBadge(RemedyRecipe recipe)
    {
        if (recipe.IsCustom || recipe.SourceKind == RecipeSourceKind.Custom)
        {
            return ("\u2605 Custom", "#F59E0B");
        }

        return recipe.SourceKind == RecipeSourceKind.Online
            ? ("\U0001F310 Found Online", "#2563EB")
            : ("\U0001F4D6 Local Library", "#0EA5E9");
    }

    private static Grid BuildQuantityRow(TextBox quantity, TextBox people)
    {
        var grid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.Children.Add(new TextBlock { Text = "Final Quantity Required", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(quantity, 1);
        grid.Children.Add(quantity);
        var peopleLabel = new TextBlock { Text = "Number of People", Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(peopleLabel, 2);
        grid.Children.Add(peopleLabel);
        Grid.SetColumn(people, 3);
        grid.Children.Add(people);
        return grid;
    }

    private RemedyRecipe ReadRecipeFromCard(RemedyRecipe original, TextBox quantity, TextBox people, IEnumerable<IngredientEditorRow> rows)
    {
        var recipe = original.Clone();
        recipe.FinalQuantity = ParseDouble(quantity.Text, original.FinalQuantity);
        recipe.People = Math.Max(1, (int)Math.Round(ParseDouble(people.Text, original.People)));
        recipe.Ingredients = rows.Select(row => row.Read()).ToList();
        recipe.SourceKind = original.SourceKind;
        return recipe;
    }

    private RemedyRecipe BuildCustomRecipe() => new()
    {
        Title = string.IsNullOrWhiteSpace(_customName.Text) ? "Custom remedy recipe" : _customName.Text.Trim(),
        Purpose = string.IsNullOrWhiteSpace(_customPurpose.Text) ? "Minor ailment comfort recipe." : _customPurpose.Text.Trim(),
        IsCustom = true,
        SourceKind = RecipeSourceKind.Custom,
        SafetyNote = string.IsNullOrWhiteSpace(_customSafety.Text) ? Disclaimer : _customSafety.Text.Trim(),
        FinalQuantity = ParseDouble(_customQuantity.Text, 1),
        People = Math.Max(1, (int)Math.Round(ParseDouble(_customPeople.Text, 1))),
        Ingredients = _customRows.Select(row => row.Read()).Where(item => !string.IsNullOrWhiteSpace(item.Name)).ToList(),
        Steps = SplitLines(_customSteps.Text),
        SourceNotes = SplitLines(_customSources.Text),
        SourceLinks = []
    };

    private void SaveCustomRecipe()
    {
        var recipe = BuildCustomRecipe();
        if (recipe.Ingredients.Count == 0)
        {
            MessageBox.Show("Add at least one ingredient before saving.", "Safe Home Remedy Recipes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        recipe.IsCustom = true;
        _settings.Presets.CustomRemedyRecipes.RemoveAll(existing => existing.Title.Equals(recipe.Title, StringComparison.OrdinalIgnoreCase));
        _settings.Presets.CustomRemedyRecipes.Insert(0, recipe);
        _settings.Save();
        ResetCustomForm();
        _status.Text = "Custom recipe saved as a light-gold card. Click the card title to open or close it.";
        RenderWelcome();
    }

    private void ResetCustomForm()
    {
        _customName.Clear();
        _customPurpose.Clear();
        _customSafety.Clear();
        _customQuantity.Text = "1";
        _customPeople.Text = "1";
        _customSteps.Clear();
        _customSources.Clear();
        _customRows.Clear();
        _customIngredients.Children.Clear();
        AddCustomIngredient();
    }

    private void AddCustomIngredient()
    {
        var row = new IngredientEditorRow(new RemedyIngredient(), allowRemove: true);
        row.RemoveRequested += (_, _) =>
        {
            _customRows.Remove(row);
            _customIngredients.Children.Remove(row.Root);
        };
        _customRows.Add(row);
        _customIngredients.Children.Add(row.Root);
    }

    private static List<string> SplitLines(string text) =>
        text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static StackPanel PanelBox(string color) => new()
    {
        Background = Brush(color),
        Margin = new Thickness(0, 0, 0, 10)
    };

    private static UIElement BuildIngredientHeader(bool showRemove)
    {
        var grid = IngredientGrid();
        grid.Children.Add(HeaderCell("Ingredient", 0));
        grid.Children.Add(HeaderCell("grams", 1));
        grid.Children.Add(HeaderCell("ml", 2));
        grid.Children.Add(HeaderCell("notes", 3));
        if (showRemove)
        {
            grid.Children.Add(HeaderCell("", 4));
        }

        return grid;
    }

    private void PrintRecipe(RemedyRecipe recipe)
    {
        if (recipe.Ingredients.Count == 0)
        {
            MessageBox.Show("Add at least one ingredient before printing.", "Safe Home Remedy Recipes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var panel = BuildPrintVisual(recipe, Math.Min(dialog.PrintableAreaWidth, 620));
        ReceiptPreviewWindow.PrintVisual(dialog, panel, recipe.Title);
    }

    private static FrameworkElement BuildPrintVisual(RemedyRecipe recipe, double width)
    {
        var panel = new StackPanel { Width = width, Background = Brushes.White, Margin = new Thickness(0) };
        panel.Children.Add(new TextBlock { Text = recipe.Title, FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 12, 12, 4) });
        panel.Children.Add(new TextBlock { Text = SourceBadge(recipe).Label, FontSize = 11, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, Margin = new Thickness(12, 0, 12, 3) });
        panel.Children.Add(new TextBlock { Text = recipe.Purpose, FontSize = 13, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 0, 12, 8) });
        panel.Children.Add(PrintSection("Safety", [recipe.SafetyNote]));
        panel.Children.Add(PrintSection("Quantity", [$"Final quantity: {recipe.FinalQuantity:0.##} | People: {recipe.People}"]));
        panel.Children.Add(PrintSection("Ingredients", recipe.Ingredients.Select(FormatIngredient).ToList()));
        panel.Children.Add(PrintSection("Instructions", recipe.Steps.Select((step, index) => $"{index + 1}. {step}").ToList()));
        if (recipe.SourceNotes.Count > 0 || recipe.SourceLinks.Count > 0)
        {
            panel.Children.Add(PrintSection("Source notes", recipe.SourceNotes.Concat(recipe.SourceLinks).ToList()));
        }
        panel.Children.Add(new TextBlock { Text = Disclaimer, FontSize = 9, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(12, 10, 12, 12) });
        return panel;
    }

    private static Border PrintSection(string title, IReadOnlyList<string> lines)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 3) });
        foreach (var line in lines)
        {
            stack.Children.Add(new TextBlock { Text = line, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) });
        }

        return new Border
        {
            Child = stack,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(12, 7, 12, 5)
        };
    }

    private static string FormatIngredient(RemedyIngredient ingredient)
    {
        var amount = ingredient.Grams.HasValue
            ? $"{ingredient.Grams:0.##} g"
            : ingredient.Ml.HasValue
                ? $"{ingredient.Ml:0.##} ml"
                : "";
        var note = string.IsNullOrWhiteSpace(ingredient.Notes) ? "" : $" - {ingredient.Notes}";
        return string.IsNullOrWhiteSpace(amount)
            ? $"{ingredient.Name}{note}"
            : $"{ingredient.Name}: {amount}{note}";
    }

    private static TextBox NumericBox(string text) => new()
    {
        Text = text,
        Width = 82,
        MinHeight = 28,
        Padding = new Thickness(6, 2, 6, 2)
    };

    private static TextBox MultilineBox(double height) => new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        MinHeight = height
    };

    private static UIElement Field(string label, TextBox box)
    {
        var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(box, 1);
        grid.Children.Add(box);
        return grid;
    }

    private static Border Notice(string text) => new()
    {
        Background = Brush("#FFF7ED"),
        BorderBrush = Brush("#FDBA74"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Margin = new Thickness(0, 0, 0, 8),
        Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Foreground = Brush("#9A3412") }
    };

    private static Grid IngredientGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        return grid;
    }

    private static TextBlock HeaderCell(string text, int column)
    {
        var block = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold, Opacity = 0.72, Margin = new Thickness(4, 0, 4, 2) };
        Grid.SetColumn(block, column);
        return block;
    }

    private static Button Button(string label, string color, RoutedEventHandler handler, double width)
    {
        var button = new Button
        {
            Content = label,
            Width = width,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Background = Brush(color),
            BorderBrush = Brushes.LightGray,
            FontWeight = FontWeights.SemiBold
        };
        button.Click += handler;
        return button;
    }

    private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;

    private static double ParseDouble(string value, double fallback) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number
            : fallback;

    private static string Shorten(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..(maxLength - 3)]}...";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private const string SystemPrompt = """
        You are a safe recipe-only assistant for minor home-comfort remedies.
        Return only a JSON array of up to 3 objects with:
        title, purpose, safetyNote, finalQuantity, people, ingredients, steps, sourceNotes, sourceLinks.
        ingredients must be objects with name, grams, ml, notes. Use either grams or ml, not both.
        Do not ask for, store, infer, or output personal information.
        Do not diagnose, treat, cure, prevent disease, replace prescribed medicine, or give emergency advice.
        Block eye/ear essential oils, homemade eye washes, ingesting unknown herbs, serious illness, pregnancy, children, pets, wounds, burns, infection, allergic reactions, or medication-interaction concerns.
        For eye wash, say only sterile saline or professional care.
        Keep output concise, modest, and recipe-like.
        """;

    private static readonly string[] PersonalInfoTerms =
    [
        "my name", "patient", "nhs", "date of birth", "dob", "phone number", "mobile number", "email address"
    ];

    private static readonly string[] EyeTerms =
    [
        "eye", "eyes", "eyewash", "eye wash", "conjunctivitis", "pink eye", "stye"
    ];

    private static readonly string[] HardBlockedTerms =
    [
        "diagnose", "cure", "treat", "treatment", "prescribed", "prescription", "replace medicine", "stop medicine",
        "infection", "infected", "wound", "burn", "poison", "poisoning", "pregnant", "pregnancy", "baby", "child",
        "children", "toddler", "pet", "dog", "cat", "diabetes", "asthma", "blood pressure", "chest pain",
        "shortness of breath", "fever", "seizure", "allergic reaction", "anaphylaxis", "ear", "ears",
        "swallow essential oil", "drink essential oil", "ingest essential oil", "unknown herb", "bleach", "borax"
    ];

    private static readonly string[][] SearchConceptGroups =
    [
        ["salt bath", "saline", "salt water", "salt-water", "saline soak", "salt soak", "salt rinse"],
        ["foot deodorant", "foot odour", "foot odor", "shoe deodorant", "shoe freshener", "foot powder", "sweaty feet"],
        ["oatmeal bath", "oat soak", "oatmeal soak", "colloidal oatmeal", "dry skin comfort"],
        ["honey lemon", "honey drink", "lemon honey", "throat comfort", "warm honey"],
        ["ginger drink", "ginger tea", "nausea comfort", "digestive comfort", "warming drink"],
        ["hand scrub", "sugar scrub", "gentle scrub", "exfoliating scrub"],
        ["warm compress", "heat pack", "warm cloth", "muscle comfort"],
        ["cool compress", "cold compress", "cool cloth", "minor swelling comfort"],
        ["steam", "steam bowl", "humidifier", "nasal comfort", "dry nose"],
        ["baking soda", "sodium bicarbonate", "deodorizing powder"],
        ["vinegar", "white vinegar", "shoe wipe", "deodorizing wipe"]
    ];

    private static readonly string[] SearchVocabulary = SearchConceptGroups
        .SelectMany(group => group)
        .SelectMany(Tokens)
        .Concat([
            "SALT", "BATH", "SALINE", "FOOT", "ODOR", "ODOUR", "THROAT", "COMFORT", "OATMEAL", "OAT",
            "GINGER", "HONEY", "LEMON", "COMPRESS", "HUMIDIFIER", "NASAL", "SKIN", "DRY", "ITCH",
            "CHAPPED", "LIP", "HAND", "SCRUB", "VINEGAR", "BAKING", "SODA", "POWDER", "SPRAY"
        ])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private sealed record QuickSearch(string Label, string Query, string Color);

    private static readonly QuickSearch[] QuickSearches =
    [
        new("Salt / saline", "salt bath saline comfort", "#E0F2FE"),
        new("Throat comfort", "honey lemon throat comfort", "#FEF3C7"),
        new("Foot odour", "foot deodorant sweaty feet", "#ECFDF5"),
        new("Dry skin", "oatmeal dry skin comfort", "#F5F3FF"),
        new("Compress", "warm compress muscle comfort", "#FFEDD5"),
        new("Nasal dryness", "humidified air dry nose", "#E0F7FA"),
        new("Hand care", "gentle sugar hand scrub", "#FCE7F3"),
        new("Digestive comfort", "ginger comfort drink", "#FFF7ED")
    ];

    private static readonly RemedyRecipe[] OfflineRecipes =
    [
        new()
        {
            Title = "Simple salt-water rinse",
            Purpose = "General mouth or throat comfort for minor irritation.",
            SearchAliases = ["saline", "salt bath", "salt water", "salt rinse", "gargle", "throat comfort"],
            SafetyNote = "Do not use for severe pain, fever, wounds, infection, after surgery, or as a treatment.",
            FinalQuantity = 250,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Warm drinking water", Ml = 250, Notes = "Comfortably warm, not hot." },
                new RemedyIngredient { Name = "Table salt", Grams = 1.5, Notes = "About 1/4 teaspoon." }
            ],
            Steps =
            [
                "Stir salt into warm water until fully dissolved.",
                "Use as a gentle rinse and spit out.",
                "Make fresh each time."
            ],
            SourceNotes =
            [
                "Saline rinses are commonly used for gentle cleansing and comfort; no cure claim."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Saline_(medicine)"
            ]
        },
        new()
        {
            Title = "Baking-soda foot deodorizing powder",
            Purpose = "Low-risk odor control for shoes or intact dry feet.",
            SearchAliases = ["foot odour", "foot odor", "sweaty feet", "shoe powder", "deodorant", "bicarb"],
            SafetyNote = "External use only. Avoid broken, irritated, infected skin and stop if irritation occurs.",
            FinalQuantity = 40,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Baking soda", Grams = 20, Notes = "Odor absorbing powder." },
                new RemedyIngredient { Name = "Cornstarch or arrowroot", Grams = 20, Notes = "Moisture absorbing powder." },
                new RemedyIngredient { Name = "Lavender essential oil", Notes = "Optional: 1 drop only, mixed thoroughly. Never ingest." }
            ],
            Steps =
            [
                "Mix dry powders in a clean jar.",
                "If using essential oil, add only one drop and stir thoroughly.",
                "Dust lightly inside shoes or on dry intact feet."
            ],
            SourceNotes =
            [
                "Baking soda can absorb odor and moisture; essential-oil evidence for odor control is limited."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Sodium_bicarbonate"
            ]
        },
        new()
        {
            Title = "Diluted shoe freshening spray",
            Purpose = "Aromatic shoe freshener, not a skin treatment.",
            SearchAliases = ["shoe deodorant", "foot odour", "shoe spray", "sweaty shoes", "essential oil spray"],
            SafetyNote = "Do not spray on skin, children, pets, wounds, eyes, or ears. Essential oils must not be ingested.",
            FinalQuantity = 100,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Water", Ml = 95, Notes = "Base liquid." },
                new RemedyIngredient { Name = "Clear alcohol", Ml = 5, Notes = "Vodka or isopropyl alcohol for drying." },
                new RemedyIngredient { Name = "Lavender or tea tree essential oil", Notes = "Optional: 2 drops. Can stain; patch-test shoe material." }
            ],
            Steps =
            [
                "Combine in a labelled spray bottle and shake well.",
                "Spray lightly inside shoes only.",
                "Let shoes dry fully before wearing."
            ],
            SourceNotes =
            [
                "Alcohol helps drying; laboratory antimicrobial findings do not prove clinical benefit."
            ],
            SourceLinks = []
        },
        new()
        {
            Title = "Plain oatmeal bath soak",
            Purpose = "Gentle skin comfort for non-broken skin.",
            SearchAliases = ["oat bath", "oatmeal soak", "colloidal oatmeal", "dry skin", "itch comfort"],
            SafetyNote = "Avoid broken, infected, severely irritated skin. Bath may become slippery.",
            FinalQuantity = 45,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Finely ground plain oats", Grams = 45, Notes = "No fragrance or sugar." }
            ],
            Steps =
            [
                "Grind oats into a fine powder.",
                "Sprinkle into warm bath water and stir.",
                "Soak briefly and rinse the bath afterwards."
            ],
            SourceNotes =
            [
                "Colloidal oatmeal has documented soothing and barrier-supporting properties."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Oatmeal"
            ]
        },
        new()
        {
            Title = "Honey lemon comfort drink",
            Purpose = "Warm comfort drink for adults with minor throat irritation.",
            SearchAliases = ["throat comfort", "honey drink", "lemon honey", "tickly throat", "warm drink"],
            SafetyNote = "Not for children under 1 year. Avoid if allergic or if sugar intake is restricted.",
            FinalQuantity = 250,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Warm water", Ml = 250, Notes = "Not boiling." },
                new RemedyIngredient { Name = "Honey", Grams = 10, Notes = "About 1 teaspoon." },
                new RemedyIngredient { Name = "Lemon juice", Ml = 5, Notes = "Optional, for taste." }
            ],
            Steps =
            [
                "Stir honey into warm water.",
                "Add lemon juice if wanted.",
                "Sip slowly for comfort."
            ],
            SourceNotes =
            [
                "Honey may soothe minor throat irritation; this is a comfort drink, not a treatment."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Honey"
            ]
        },
        new()
        {
            Title = "Mild saline foot soak",
            Purpose = "Minor foot comfort soak for intact skin.",
            SearchAliases = ["salt bath", "salt soak", "saline soak", "salt water bath", "foot bath"],
            SafetyNote = "Use only on intact skin. Do not use for wounds, infection, diabetes-related foot concerns, severe pain, or swelling.",
            FinalQuantity = 1000,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Warm water", Ml = 1000, Notes = "Comfortably warm, never hot." },
                new RemedyIngredient { Name = "Table salt", Grams = 6, Notes = "About 1 teaspoon for a mild saline soak." }
            ],
            Steps =
            [
                "Dissolve salt fully in warm water.",
                "Soak intact feet briefly.",
                "Dry feet thoroughly afterwards."
            ],
            SourceNotes =
            [
                "This is a comfort soak based on mild saline; it is not a treatment."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Saline_(medicine)"
            ]
        },
        new()
        {
            Title = "Warm compress cloth",
            Purpose = "Gentle warmth for minor muscle comfort or relaxation.",
            SearchAliases = ["heat pack", "warm cloth", "muscle comfort", "stiffness comfort", "aches"],
            SafetyNote = "Do not use on burns, wounds, numb skin, infection, unexplained swelling, or severe pain.",
            FinalQuantity = 250,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Warm water", Ml = 250, Notes = "Warm, not hot." },
                new RemedyIngredient { Name = "Clean cloth", Notes = "Soft cloth or towel." }
            ],
            Steps =
            [
                "Soak the cloth in warm water.",
                "Wring out excess water.",
                "Apply briefly and check skin comfort often."
            ],
            SourceNotes =
            [
                "Warmth can feel relaxing; avoid heat where symptoms are serious or unexplained."
            ],
            SourceLinks = []
        },
        new()
        {
            Title = "Cool compress cloth",
            Purpose = "Cooling comfort for minor heat or puffiness on intact skin.",
            SearchAliases = ["cold compress", "cool cloth", "puffy skin", "minor swelling comfort", "cooling"],
            SafetyNote = "Do not use for serious injury, allergic reaction, eye concerns, wounds, or worsening symptoms.",
            FinalQuantity = 250,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Cool water", Ml = 250, Notes = "Cool, not icy." },
                new RemedyIngredient { Name = "Clean cloth", Notes = "Soft cloth or towel." }
            ],
            Steps =
            [
                "Soak the cloth in cool water.",
                "Wring out excess water.",
                "Apply briefly to intact skin and stop if uncomfortable."
            ],
            SourceNotes =
            [
                "Cooling can provide comfort; seek help for serious, allergic, eye-related, or worsening concerns."
            ],
            SourceLinks = []
        },
        new()
        {
            Title = "Ginger comfort drink",
            Purpose = "Warm adult comfort drink for mild digestive unease.",
            SearchAliases = ["ginger tea", "digestive comfort", "nausea comfort", "warming drink", "stomach comfort"],
            SafetyNote = "Avoid if pregnant, on blood-thinning medication, allergic, or advised to avoid ginger.",
            FinalQuantity = 250,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Warm water", Ml = 250, Notes = "Not boiling." },
                new RemedyIngredient { Name = "Fresh ginger slice", Grams = 2, Notes = "Small thin slice; remove before drinking." },
                new RemedyIngredient { Name = "Honey", Grams = 5, Notes = "Optional for taste." }
            ],
            Steps =
            [
                "Steep the ginger slice in warm water for a few minutes.",
                "Remove the ginger.",
                "Add honey if wanted and sip slowly."
            ],
            SourceNotes =
            [
                "Ginger has some evidence for nausea contexts, but this card is only a mild comfort drink and not medical advice."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Ginger"
            ]
        },
        new()
        {
            Title = "White-vinegar shoe deodorizing wipe",
            Purpose = "Simple shoe-surface deodorizing wipe.",
            SearchAliases = ["vinegar shoe", "shoe odour", "shoe odor", "deodorizing wipe", "shoe wipe"],
            SafetyNote = "Use on shoe surfaces only. Do not apply to skin, wounds, children, pets, eyes, or ears.",
            FinalQuantity = 100,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Water", Ml = 80, Notes = "Dilution base." },
                new RemedyIngredient { Name = "White vinegar", Ml = 20, Notes = "Patch-test shoe material first." }
            ],
            Steps =
            [
                "Mix water and vinegar.",
                "Dampen a cloth lightly.",
                "Wipe inside shoe surfaces and allow to dry fully."
            ],
            SourceNotes =
            [
                "Vinegar can reduce some odours on surfaces; it may damage materials, so patch-test first."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Vinegar"
            ]
        },
        new()
        {
            Title = "Plain humidified-air comfort",
            Purpose = "Moist air comfort for a dry room or dry nose feeling.",
            SearchAliases = ["humidifier", "dry nose", "nasal dryness", "steam alternative", "dry air"],
            SafetyNote = "Do not use steam near children, burns risk, breathing difficulty, fever, or persistent symptoms.",
            FinalQuantity = 1,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Clean humidifier or bowl of warm water", Notes = "Avoid boiling steam and keep equipment clean." }
            ],
            Steps =
            [
                "Use a clean humidifier according to its instructions, or place warm water safely away from edges.",
                "Do not lean over boiling water.",
                "Clean and dry equipment after use."
            ],
            SourceNotes =
            [
                "Moist air can improve comfort in dry environments; avoid steam-burn risk."
            ],
            SourceLinks = []
        },
        new()
        {
            Title = "Gentle sugar hand scrub",
            Purpose = "Simple hand exfoliating recipe for intact, non-irritated skin.",
            SearchAliases = ["hand scrub", "sugar scrub", "dry hands", "rough hands", "gentle scrub"],
            SafetyNote = "Do not use on cracked, sore, broken, inflamed, or irritated skin. Stop if stinging occurs.",
            FinalQuantity = 20,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Fine sugar", Grams = 15, Notes = "Gentle mechanical exfoliant." },
                new RemedyIngredient { Name = "Olive or sunflower oil", Ml = 5, Notes = "Use a simple food-grade oil." }
            ],
            Steps =
            [
                "Mix sugar and oil into a soft paste.",
                "Massage very gently over intact hands for 20 to 30 seconds.",
                "Rinse well and dry hands."
            ],
            SourceNotes =
            [
                "This is cosmetic skin care for intact skin, not a treatment."
            ],
            SourceLinks = []
        },
        new()
        {
            Title = "Glycerin hand comfort mix",
            Purpose = "Short-use hand moisturising mix for dry intact skin.",
            SearchAliases = ["dry hands", "glycerin", "hand moisturiser", "chapped hands", "skin comfort"],
            SafetyNote = "Use fresh and externally only. Avoid broken, inflamed, infected, or very sensitive skin.",
            FinalQuantity = 30,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Glycerin", Ml = 5, Notes = "Humectant; use a small amount." },
                new RemedyIngredient { Name = "Clean water", Ml = 25, Notes = "Make fresh; do not store." }
            ],
            Steps =
            [
                "Mix glycerin and water in a clean cup.",
                "Apply a small amount to clean intact hands.",
                "Discard leftovers rather than storing."
            ],
            SourceNotes =
            [
                "Glycerin is a common humectant in skin products; this fresh mix avoids preservative/storage issues."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Glycerol"
            ]
        },
        new()
        {
            Title = "Simple lip comfort balm",
            Purpose = "Basic barrier balm for dry lips.",
            SearchAliases = ["dry lips", "chapped lips", "lip balm", "lip comfort", "beeswax"],
            SafetyNote = "External use only. Avoid if allergic to beeswax, coconut, olive oil, or any ingredient.",
            FinalQuantity = 15,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Beeswax", Grams = 5, Notes = "Barrier ingredient." },
                new RemedyIngredient { Name = "Coconut oil or olive oil", Ml = 10, Notes = "Simple oil base." }
            ],
            Steps =
            [
                "Melt beeswax gently using low heat.",
                "Stir in oil and pour into a clean small container.",
                "Let set before use."
            ],
            SourceNotes =
            [
                "Barrier balms reduce moisture loss; avoid use if irritation or allergy occurs."
            ],
            SourceLinks =
            [
                "https://en.wikipedia.org/wiki/Beeswax"
            ]
        },
        new()
        {
            Title = "Rice sock warm pack",
            Purpose = "Reusable dry warmth pack for minor comfort.",
            SearchAliases = ["heat pack", "rice pack", "warm pack", "muscle comfort", "period comfort"],
            SafetyNote = "Burn risk. Do not use on numb skin, children, pregnancy-related pain, severe pain, swelling, wounds, or infection.",
            FinalQuantity = 250,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Uncooked rice", Grams = 250, Notes = "Fill a clean cotton sock or pouch." },
                new RemedyIngredient { Name = "Clean cotton sock", Notes = "Tie securely." }
            ],
            Steps =
            [
                "Fill the sock with rice and tie securely.",
                "Warm briefly in a microwave in short intervals.",
                "Test on the inside of the wrist before applying over clothing."
            ],
            SourceNotes =
            [
                "Dry warmth can feel relaxing; careful temperature testing is essential."
            ],
            SourceLinks = []
        },
        new()
        {
            Title = "Plain menthol-free vapour bowl",
            Purpose = "Room vapour comfort without essential oils.",
            SearchAliases = ["steam", "vapour", "vapor", "nasal comfort", "dry air", "blocked nose comfort"],
            SafetyNote = "Scald risk. Do not lean over boiling water. Avoid for children, breathing difficulty, fever, asthma, or persistent symptoms.",
            FinalQuantity = 500,
            People = 1,
            Ingredients =
            [
                new RemedyIngredient { Name = "Hot water", Ml = 500, Notes = "Place safely away from edges; do not inhale close steam." }
            ],
            Steps =
            [
                "Place hot water in a stable bowl away from edges.",
                "Sit nearby for room moisture comfort.",
                "Do not add essential oils and do not lean over the bowl."
            ],
            SourceNotes =
            [
                "This avoids essential oils and close steam exposure; humidified air may feel comforting in dry rooms."
            ],
            SourceLinks = []
        }
    ];
}

public sealed class IngredientEditorRow
{
    private readonly RemedyIngredient _baseIngredient;
    private readonly TextBox _name = new() { MinHeight = 28, Padding = new Thickness(5, 2, 5, 2) };
    private readonly TextBox _grams = new() { MinHeight = 28, Padding = new Thickness(5, 2, 5, 2) };
    private readonly TextBox _ml = new() { MinHeight = 28, Padding = new Thickness(5, 2, 5, 2) };
    private readonly TextBox _notes = new() { MinHeight = 28, Padding = new Thickness(5, 2, 5, 2) };
    private bool _scaling;

    public event EventHandler? RemoveRequested;
    public Grid Root { get; }

    public IngredientEditorRow(RemedyIngredient ingredient, bool allowRemove)
    {
        _baseIngredient = ingredient.Clone();
        Root = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });

        _name.Text = ingredient.Name;
        _grams.Text = ingredient.Grams?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        _ml.Text = ingredient.Ml?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        _notes.Text = ingredient.Notes;

        Add(_name, 0);
        Add(_grams, 1);
        Add(_ml, 2);
        Add(_notes, 3);

        if (allowRemove)
        {
            var remove = new Button { Content = "Remove", Height = 28, Margin = new Thickness(4, 0, 0, 0), Background = Brush("#FFEAEA"), BorderBrush = Brushes.LightGray };
            remove.Click += (_, _) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            Add(remove, 4);
        }

        _grams.TextChanged += (_, _) => UpdateMutualAmountState();
        _ml.TextChanged += (_, _) => UpdateMutualAmountState();
        UpdateMutualAmountState();
    }

    public void ApplyScale(double scale)
    {
        _scaling = true;
        if (_baseIngredient.Grams.HasValue)
        {
            _grams.Text = (_baseIngredient.Grams.Value * scale).ToString("0.##", CultureInfo.InvariantCulture);
        }

        if (_baseIngredient.Ml.HasValue)
        {
            _ml.Text = (_baseIngredient.Ml.Value * scale).ToString("0.##", CultureInfo.InvariantCulture);
        }

        _scaling = false;
        UpdateMutualAmountState();
    }

    public RemedyIngredient Read() => new()
    {
        Name = _name.Text.Trim(),
        Grams = ParseNullable(_grams.Text),
        Ml = ParseNullable(_ml.Text),
        Notes = _notes.Text.Trim()
    };

    private void UpdateMutualAmountState()
    {
        if (_scaling)
        {
            return;
        }

        var hasGrams = !string.IsNullOrWhiteSpace(_grams.Text);
        var hasMl = !string.IsNullOrWhiteSpace(_ml.Text);
        _ml.IsEnabled = !hasGrams;
        _grams.IsEnabled = !hasMl;
        _ml.Background = hasGrams ? Brush("#EEF2F7") : Brushes.White;
        _grams.Background = hasMl ? Brush("#EEF2F7") : Brushes.White;
    }

    private void Add(UIElement element, int column)
    {
        if (element is Control control)
        {
            control.Margin = new Thickness(3, 0, 3, 0);
        }

        Grid.SetColumn(element, column);
        Root.Children.Add(element);
    }

    private static double? ParseNullable(string value) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number
            : null;

    private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;
}

public sealed class RemedyRecipe
{
    public string Title { get; set; } = "";
    public string Purpose { get; set; } = "";
    public List<string> SearchAliases { get; set; } = [];
    public bool IsCustom { get; set; }
    public RecipeSourceKind SourceKind { get; set; } = RecipeSourceKind.Local;
    public string SafetyNote { get; set; } = "";
    public double FinalQuantity { get; set; } = 1;
    public int People { get; set; } = 1;
    public List<RemedyIngredient> Ingredients { get; set; } = [];
    public List<string> Steps { get; set; } = [];
    public List<string> SourceNotes { get; set; } = [];
    public List<string> SourceLinks { get; set; } = [];

    public RemedyRecipe Clone() => new()
    {
        Title = Title,
        Purpose = Purpose,
        SearchAliases = SearchAliases.ToList(),
        IsCustom = IsCustom,
        SourceKind = SourceKind,
        SafetyNote = SafetyNote,
        FinalQuantity = FinalQuantity,
        People = People,
        Ingredients = Ingredients.Select(ingredient => ingredient.Clone()).ToList(),
        Steps = Steps.ToList(),
        SourceNotes = SourceNotes.ToList(),
        SourceLinks = SourceLinks.ToList()
    };
}

public enum RecipeSourceKind
{
    Local,
    Online,
    Custom
}

public sealed class RemedyIngredient
{
    public string Name { get; set; } = "";
    public double? Grams { get; set; }
    public double? Ml { get; set; }
    public string Notes { get; set; } = "";

    public RemedyIngredient Clone() => new()
    {
        Name = Name,
        Grams = Grams,
        Ml = Ml,
        Notes = Notes
    };
}
