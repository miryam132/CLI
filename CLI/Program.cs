
using System.CommandLine;

RootCommand rootCommand = new RootCommand("My Code Bundler CLI");

Command bundleCommand = new Command("bundle", "Combine code files into one file");

Option<string> languageOption = new Option<string>("--language", "-l")
{
    Description = "Programming languages to include (or all)",
    Required = true
};

languageOption.Validators.Add(result =>
{
    string? value = result.GetValueOrDefault<string>();

    if (string.IsNullOrWhiteSpace(value))
    {
        result.AddError("You must specify at least one language.");
        return;
    }

    string[] selectedLanguages = value.Split(',');

    Dictionary<string, string[]> languages = GetLanguages();

    foreach (string language in selectedLanguages)
    {
        string currentLanguage = language.Trim();

        if (currentLanguage.Length == 0)
        {
            result.AddError("Language names cannot be empty.");
            return;
        }

        if (currentLanguage.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (selectedLanguages.Length > 1){
                result.AddError("'all' must be used alone.");
            }

            return;
        }

        if (!languages.ContainsKey(currentLanguage))
        {
            result.AddError($"Unsupported language: {currentLanguage}");
            return;
        }
    }
});


Option<string> outputOption = new Option<string>("--output", "-o")
{
    Description = "The output file name or full path"
};

Option<bool> noteOption = new Option<bool>("--note", "-n")
{
    Description = "Include source file paths as comments"
};

Option<string> sortOption = new Option<string>("--sort", "-s")
{
    Description = "Sort files by name or type",
    DefaultValueFactory = _ => "name"
};


sortOption.Validators.Add(result =>
{
    string? value = result.GetValueOrDefault<string>();

    if (value != "name" && value != "type")
    {
        result.AddError("Sort must be 'name' or 'type'.");
    }
});


Option<bool> removeEmptyLinesOption =
    new Option<bool>("--remove-empty-lines", "-r")
    {
        Description = "Remove empty lines from source files"
    };

Option<string> authorOption = new Option<string>("--author", "-a")
{
    Description = "The name of the bundle file author"
};

bundleCommand.Options.Add(authorOption);

bundleCommand.Options.Add(removeEmptyLinesOption);

bundleCommand.Options.Add(sortOption);

bundleCommand.Options.Add(noteOption);

bundleCommand.Options.Add(outputOption);

bundleCommand.Options.Add(languageOption);


bundleCommand.SetAction(parseResult =>
{
    string language = parseResult.GetValue(languageOption)!;
    string? output = parseResult.GetValue(outputOption);
    bool note = parseResult.GetValue(noteOption);
    string sort = parseResult.GetValue(sortOption)!;
    bool removeEmptyLines = parseResult.GetValue(removeEmptyLinesOption);
    string? author = parseResult.GetValue(authorOption);

    Console.WriteLine($"Language: {language}");
    Console.WriteLine($"Output: {output}");
    Console.WriteLine($"Note: {note}");
    Console.WriteLine($"Sort: {sort}");
    Console.WriteLine($"Remove empty lines: {removeEmptyLines}");
    Console.WriteLine($"Author: {author}");


    string currentDirectory = Directory.GetCurrentDirectory();

    List<string> files = GetProjectFiles(currentDirectory);

    files = FilterFilesByLanguage(files, language);

    if (string.IsNullOrWhiteSpace(output))
    {
        output = "bundle.txt";
    }

    string outputPath = Path.GetFullPath(output);

    if (files.Any(file =>
        Path.GetFullPath(file).Equals(
            outputPath,
            StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine(
            "Error: The output file cannot be one of the source files.");
        return;
    }

    try
    {
        if (File.Exists(outputPath))
        {
            Console.WriteLine(
                $"Warning: The file '{outputPath}' already exists.");

            Console.Write("Overwrite it? (y/n): ");
            string? answer = Console.ReadLine();

            if (!string.Equals(
                answer,
                "y",
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Bundle creation cancelled.");
                return;
            }
        }

        CreateBundle(
            files,outputPath, currentDirectory,
            note, sort,removeEmptyLines,author
        );
    }
    catch (DirectoryNotFoundException)
    {
        Console.Error.WriteLine(
            "Error: The output directory does not exist.");
    }
    catch (UnauthorizedAccessException)
    {
        Console.Error.WriteLine(
            "Error: Access denied. Check file permissions.");
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine(
            $"File error: {ex.Message}");
    }
});

Command createRspCommand = new Command("create-rsp", "Create a response file for bundle");


createRspCommand.SetAction(parseResult =>
{
    Console.Write("Response file name (without extension): ");
    string? fileName = Console.ReadLine();

    Console.Write("Languages (e.g. csharp,python or all): ");
    string? languages = Console.ReadLine();

    Console.Write("Output file (Enter for bundle.txt): ");
    string? output = Console.ReadLine();

    Console.Write("Include source notes? (y/n): ");
    string? note = Console.ReadLine();

    Console.Write("Sort by name or type? (Enter for name): ");
    string? sort = Console.ReadLine();

    Console.Write("Remove empty lines? (y/n): ");
    string? removeEmptyLines = Console.ReadLine();

    Console.Write("Author (optional): ");
    string? author = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(fileName) ||
        string.IsNullOrWhiteSpace(languages))
    {
        Console.Error.WriteLine(
            "Error: Response file name and languages are required.");
        return;
    }

    if (string.IsNullOrWhiteSpace(output))
        output = "bundle.txt";

    if (string.IsNullOrWhiteSpace(sort))
        sort = "name";

    if (sort != "name" && sort != "type")
    {
        Console.Error.WriteLine(
            "Error: Sort must be 'name' or 'type'.");
        return;
    }

    string[] selectedLanguages = languages.Split(
        ',',
        StringSplitOptions.TrimEntries
    );

    Dictionary<string, string[]> supportedLanguages = GetLanguages();

    if (selectedLanguages.Any(lang => lang.Length == 0) ||
        (selectedLanguages.Length > 1 &&
         selectedLanguages.Contains(
             "all",
             StringComparer.OrdinalIgnoreCase)) ||
        selectedLanguages.Any(lang =>
            !lang.Equals("all", StringComparison.OrdinalIgnoreCase) &&
            !supportedLanguages.ContainsKey(lang)))
    {
        Console.Error.WriteLine(
            "Error: Invalid language selection.");
        return;
    }

    List<string> arguments = new List<string>
    {
        "bundle",
        "--language",
        languages,
        "--output",
        output,
        "--sort",
        sort
    };

    if (note?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        arguments.Add("--note");

    if (removeEmptyLines?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        arguments.Add("--remove-empty-lines");

    if (!string.IsNullOrWhiteSpace(author))
    {
        arguments.Add("--author");
        arguments.Add(author);
    }

    string rspPath = Path.GetFullPath(fileName + ".rsp");

    File.WriteAllLines(
        rspPath,
        arguments.Select(arg =>
            arg.Contains(' ') ? $"\"{arg}\"" : arg)
    );

    Console.WriteLine($"Response file created: {rspPath}");
});

rootCommand.Subcommands.Add(bundleCommand);
rootCommand.Subcommands.Add(createRspCommand);

return rootCommand.Parse(args).Invoke();


static List<string> GetProjectFiles(string directoryPath)
{
    List<string> files = new List<string>();

    string[] excludedFolders =
    {
        "bin",
        "obj",
        "Debug",
        "Release",
        ".git",
        "node_modules"
    };

    foreach (string file in Directory.GetFiles(directoryPath))
    {
        files.Add(file);
    }

    foreach (string directory in Directory.GetDirectories(directoryPath))
    {
        string folderName = Path.GetFileName(directory);

        if (excludedFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
        {
            continue;
        }

        files.AddRange(GetProjectFiles(directory));
    }

    return files;
}

static Dictionary<string, string[]> GetLanguages()
{
    return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "csharp", new[] { ".cs" } },
        { "java", new[] { ".java" } },
        { "python", new[] { ".py" } },
        { "javascript", new[] { ".js", ".jsx" } },
        { "typescript", new[] { ".ts", ".tsx" } },
        { "html", new[] { ".html", ".htm" } },
        { "css", new[] { ".css" } },
        { "cpp", new[] { ".cpp", ".h", ".hpp" } },
        { "c", new[] { ".c" } },
        { "sql", new[] { ".sql" } }
    };
}

static List<string> FilterFilesByLanguage(
    List<string> files,
    string language)
{
    Dictionary<string, string[]> languages = GetLanguages();

    string[] selectedLanguages = language.Split(
        ',',
        StringSplitOptions.TrimEntries |
        StringSplitOptions.RemoveEmptyEntries
    );

    List<string> extensions = new List<string>();

    if (selectedLanguages.Contains("all", StringComparer.OrdinalIgnoreCase))
    {
        extensions = languages.Values
            .SelectMany(value => value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    else
    {
        foreach (string selectedLanguage in selectedLanguages)
        {
            if (languages.TryGetValue(selectedLanguage, out string[]? languageExtensions))
            {
                extensions.AddRange(languageExtensions);
            }
        }
    }

    return files
        .Where(file => extensions.Contains(
            Path.GetExtension(file),
            StringComparer.OrdinalIgnoreCase)).ToList();
}

static void CreateBundle(
    List<string> files,
    string outputPath,
    string sourceDirectory,
    bool note,
    string sort,
    bool removeEmptyLines,
    string? author)
{
    if (sort == "type") {
        files = files
            .OrderBy(file => Path.GetExtension(file))
            .ThenBy(file => Path.GetFileName(file))
            .ToList();
    }
    else
    {
        files = files
            .OrderBy(file => Path.GetFileName(file))
            .ToList();
    }

    using StreamWriter writer = new StreamWriter(outputPath);

    if (!string.IsNullOrWhiteSpace(author))
    {
        writer.WriteLine($"// Author: {author}");
        writer.WriteLine();
    }

    foreach (string file in files)
    {
        if (note){
            string relativePath =
                Path.GetRelativePath(sourceDirectory, file);

            writer.WriteLine($"// Source: {relativePath}");
        }

        string[] lines = File.ReadAllLines(file);

        foreach (string line in lines) {
            if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            writer.WriteLine(line);
        }

        writer.WriteLine();
    }
    Console.WriteLine($"Bundle created successfully: {outputPath}");
}