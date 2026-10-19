using System.CommandLine;

RootCommand rootCommand =
    new RootCommand("A CLI tool for combining multiple source code files into a single file");

Command bundleCommand=new Command("bundle", "Bundle source code files into a single file");
rootCommand.Subcommands.Add(bundleCommand);

Option<string[]> languageOption = new Option<string[]>("--language");
languageOption.Required = true;

bundleCommand.Options.Add(languageOption);

Option<string> output = new Option<string>("--output");
bundleCommand.Options.Add(output);
Option<bool> noteOption = new Option<bool>("--note");
bundleCommand.Options.Add(noteOption);
Option<string> sortOption = new Option<string>("--sort");
sortOption.DefaultValueFactory = _ => "name";
bundleCommand.Options.Add(sortOption);
Option<bool> removeEmptyLinesOption = new Option<bool>("--remove-empty-lines");
bundleCommand.Options.Add(removeEmptyLinesOption);
Option<string> authorOption = new Option<string>("--author");
bundleCommand.Options.Add(authorOption);

Command createRspCommand = new Command("create-rsp", "Create a response file interactively");
rootCommand.Subcommands.Add(createRspCommand);

//Aliases
languageOption.Aliases.Add("-l");
output.Aliases.Add("-o");
noteOption.Aliases.Add("-n");
sortOption.Aliases.Add("-s");
removeEmptyLinesOption.Aliases.Add("-r");
authorOption.Aliases.Add("-a");
//עד פה הוראות לשורת הפקודה
//bundleCommand
//מפה טיפול במה שקבלנו: דבר ראשון נתפוס את כל הקבצים שבמיקום התיקייה לתוך מערך.
bundleCommand.SetAction(parseResult =>
{
    string[] languages = parseResult.GetValue(languageOption) ?? [];

    string outputPath = parseResult.GetValue(output) ?? "";
    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.WriteLine("Error: Output path is required.");
        return;
    }

    bool note = parseResult.GetValue(noteOption);

    string sort = parseResult.GetValue(sortOption) ?? "name";
    if (!sort.Equals("name", StringComparison.OrdinalIgnoreCase) &&
    !sort.Equals("type", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Error: Sort must be 'name' or 'type'.");
        return;
    }

    bool removeEmptyLines = parseResult.GetValue(removeEmptyLinesOption);

    string author = parseResult.GetValue(authorOption) ?? "";

    string currentDirectory = Directory.GetCurrentDirectory();
    string[] files = Directory.GetFiles(currentDirectory, "*", SearchOption.AllDirectories);

    files = files.Where(file =>
    !file.Split(Path.DirectorySeparatorChar)
         .Any(folder => folder.Equals("bin", StringComparison.OrdinalIgnoreCase)
                     || folder.Equals("Debug", StringComparison.OrdinalIgnoreCase))
).ToArray();


    Dictionary<string, string> languageMap = new(StringComparer.OrdinalIgnoreCase)
{
    { "C#", "cs" },
    { "csharp", "cs" },
    { "cs", "cs" },

    { "Python", "py" },
    { "py", "py" },

    { "JavaScript", "js" },
    { "js", "js" },

    { "TypeScript", "ts" },
    { "ts", "ts" },

    { "Java", "java" },
    { "C++", "cpp" },
    { "cpp", "cpp" },
    { "C", "c" },
    { "HTML", "html" },
    { "CSS", "css" },
    { "h", "h" },
};


    foreach (string language in languages)
    {
        if (!language.Equals("all", StringComparison.OrdinalIgnoreCase)
            && !languageMap.ContainsKey(language))
        {
            Console.WriteLine($"Error: Unsupported language '{language}'");
            return;
        }
    }

    if (languages.Contains("all", StringComparer.OrdinalIgnoreCase)
    && languages.Length > 1)
    {
        Console.WriteLine("Error: 'all' cannot be combined with other languages.");
        return;
    }
    string[] supportedExtensions = languageMap.Values.Distinct().ToArray();

    files = files.Where(file =>
    supportedExtensions.Contains(
        Path.GetExtension(file).TrimStart('.'),
        StringComparer.OrdinalIgnoreCase
    )
).ToArray();

    if (!languages.Contains("all", StringComparer.OrdinalIgnoreCase))
    {
        string[] selectedExtensions = languages
            .Select(language => languageMap[language])
            .ToArray();

        files = files.Where(file =>
            selectedExtensions.Contains(
                Path.GetExtension(file).TrimStart('.'),
                StringComparer.OrdinalIgnoreCase
            )
        ).ToArray();
    }

    if (sort.Equals("type", StringComparison.OrdinalIgnoreCase))
    {
        files = files.OrderBy(file => Path.GetExtension(file)).ToArray();
    }
    else
    {
        files = files.OrderBy(file => Path.GetFileName(file)).ToArray();
    }


    try
    {


        // Exclude the output file from source files
        string fullOutputPath = Path.GetFullPath(outputPath);

        files = files.Where(file =>
            !Path.GetFullPath(file).Equals(
                fullOutputPath,
                StringComparison.OrdinalIgnoreCase
            )
        ).ToArray();


        if (files.Length == 0)
        {
            Console.WriteLine("Error: No matching source files were found.");
            return;
        }
        // Open output file
        using StreamWriter writer = new StreamWriter(outputPath);

        // Write author
        if (!string.IsNullOrWhiteSpace(author))
        {
            writer.WriteLine($"// Author: {author}");
        }

        // Combine source files
        foreach (string file in files)
        {
            // Add source file path if requested
            if (note)
            {
                writer.WriteLine($"// Source: {file}");
            }

            // Read source file
            string content = File.ReadAllText(file);

            // Remove empty lines if requested
            if (removeEmptyLines)
            {
                string[] lines = content.Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.None
                );

                content = string.Join(
                    Environment.NewLine,
                    lines.Where(line => !string.IsNullOrWhiteSpace(line))
                );
            }

            // Write source content to output
            writer.WriteLine(content);
            writer.WriteLine();
        }
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error saving output file: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"Access denied: {ex.Message}");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Invalid output path: {ex.Message}");
    }




});


createRspCommand.SetAction(parseResult =>
{
    // Languages
    Console.Write("Enter languages separated by spaces (or all): ");
    string languagesInput = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(languagesInput))
    {
        Console.WriteLine("Error: You must enter at least one language.");
        return;
    }

    string[] languages = languagesInput.Split(
        ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    );

    Dictionary<string, string> languageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "C#", "cs" },
        { "csharp", "cs" },
        { "cs", "cs" },
        { "Python", "py" },
        { "py", "py" },
        { "JavaScript", "js" },
        { "js", "js" },
        { "TypeScript", "ts" },
        { "ts", "ts" },
        { "Java", "java" },
        { "C++", "cpp" },
        { "cpp", "cpp" },
        { "C", "c" },
        { "HTML", "html" },
        { "CSS", "css" },
        { "h", "h" }
    };

    foreach (string language in languages)
    {
        if (!language.Equals("all", StringComparison.OrdinalIgnoreCase)
            && !languageMap.ContainsKey(language))
        {
            Console.WriteLine($"Error: Unsupported language '{language}'.");
            return;
        }
    }

    if (languages.Contains("all", StringComparer.OrdinalIgnoreCase)
        && languages.Length > 1)
    {
        Console.WriteLine("Error: 'all' cannot be combined with other languages.");
        return;
    }

    // Output path
    Console.Write("Enter output file path: ");
    string outputPath = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.WriteLine("Error: Output path is required.");
        return;
    }

    // Source file notes
    Console.Write("Add source file paths? (y/n): ");
    string note = (Console.ReadLine() ?? "").Trim();

    if (!note.Equals("y", StringComparison.OrdinalIgnoreCase)
        && !note.Equals("n", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Error: Enter 'y' or 'n'.");
        return;
    }

    // Sort order
    Console.Write("Sort by name or type? (name/type, Enter for name): ");
    string sort = (Console.ReadLine() ?? "").Trim();

    if (string.IsNullOrWhiteSpace(sort))
    {
        sort = "name";
    }

    if (!sort.Equals("name", StringComparison.OrdinalIgnoreCase)
        && !sort.Equals("type", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Error: Sort must be 'name' or 'type'.");
        return;
    }

    // Remove empty lines
    Console.Write("Remove empty lines? (y/n): ");
    string removeEmptyLines = (Console.ReadLine() ?? "").Trim();

    if (!removeEmptyLines.Equals("y", StringComparison.OrdinalIgnoreCase)
        && !removeEmptyLines.Equals("n", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Error: Enter 'y' or 'n'.");
        return;
    }

    // Author
    Console.Write("Enter author name (optional): ");
    string author = Console.ReadLine() ?? "";

    // Build response file arguments
    List<string> arguments = new List<string>();

    arguments.Add("--language");

    foreach (string language in languages)
    {
        arguments.Add(language);
    }

    arguments.Add("--output");
    arguments.Add($"\"{outputPath}\"");

    if (note.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        arguments.Add("--note");
    }

    arguments.Add("--sort");
    arguments.Add(sort);

    if (removeEmptyLines.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        arguments.Add("--remove-empty-lines");
    }

    if (!string.IsNullOrWhiteSpace(author))
    {
        arguments.Add("--author");
        arguments.Add($"\"{author}\"");
    }

    // Save response file
    try
    {
        File.WriteAllLines("bundle.rsp", arguments);
        Console.WriteLine("Response file created successfully!");
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error creating response file: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"Access denied: {ex.Message}");
    }
});


return rootCommand.Parse(args).Invoke();