using System.Globalization;
using System.Text;
using System.Xml.Linq;

public class MatrixLoader
{
    public static double[,] ReadMatrixFromWxs(string filePath)
    {
        try
        {
            // Read the file content
            string[] fileLines = File.ReadAllLines(filePath, Encoding.UTF8);

            // Skip the first 4 lines
            string fileContent = string.Join("\n", fileLines.Skip(4)).Trim();

            // Load the .wxs file from string content
            XDocument doc = XDocument.Parse(fileContent);

            // Use the XML namespace defined in the .wxs file
            XNamespace ns = "http://schemas.microsoft.com/wix/2006/wi";

            // Extract the Matrix element
            var matrixElement = doc.Descendants(ns + "Matrix").FirstOrDefault();
            if (matrixElement == null)
            {
                throw new Exception("No Matrix element found in the .wxs file.");
            }

            // Extract the rows
            var rows = matrixElement.Elements(ns + "Row")
                .Select(row => row.Value.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .ToList();

            // Remove the first row if it does not contain numbers
            if (rows.Count > 0 && !rows[0].All(value => double.TryParse(value, out _)))
            {
                rows.RemoveAt(0);
            }

            // Parse rows into a matrix
            var numericRows = rows.Select(row => row
                .Select(value => double.Parse(value, CultureInfo.InvariantCulture))
                .ToArray())
                .ToArray();

            // Determine the dimensions of the matrix
            int rowCount = numericRows.Length;
            int colCount = numericRows[0].Length;

            // Convert to 2D array
            double[,] matrix = new double[rowCount, colCount];
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    matrix[i, j] = numericRows[i][j];
                }
            }

            return matrix;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading matrix from .wxs file: {ex.Message}");
            return null;
        }
    }
}

