import java.util.*;
import java.io.*;

public class WordBank{
    public static void main(String[] args) throws IOException{
        Scanner s = new Scanner (new File("WordBank.txt"));
        while (s.hasNextLine()){
            System.out.println("allPhrases.Add(\"" + s.nextLine() + "\");");
        }
        s.close();
    }
}