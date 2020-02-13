using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour {
    /* Attach this to the camera object.
     * This object will draw every line that it is stored inside of it.
     * To add a new line to draw, use the AddLine(...) function.
     * To remove a line use the RemoveLine(...) function.
     */

    public Material lineMat;

    public const int ILLEGAL_LINE_HASH = 0;
    private static int previousLineName = ILLEGAL_LINE_HASH;
    private static Dictionary<int, NocabLine> lines = new Dictionary<int, NocabLine>();

    private struct NocabLine {
        // Represents a line defined by two points
        public Vector2 startPt;
        public Vector2 endPt;
        public Color color;

        public NocabLine(Vector2 startPt, Vector2 endPt, Color color) {
            this.startPt = startPt;
            this.endPt = endPt;
            this.color = color;
        }
    }

    private static class NocabLinePooler {

        private static List<NocabLine> freeLines = new List<NocabLine>();

        public static NocabLine getLine() {
            return getLine(new Vector2(0, 0), new Vector2(0, 0));
        }

        public static NocabLine getLine(Vector2 startPos, Vector2 endPos, Color color) {
            if (freeLines.Count > 0) {
                NocabLine result = freeLines[freeLines.Count - 1];
                freeLines.RemoveAt(freeLines.Count - 1);
                result.startPt = startPos;
                result.endPt = endPos;
                return result;
            } else {
                return new NocabLine(startPos, endPos, color);
            }
        }

        public static NocabLine getLine(Vector2 startPos, Vector2 endPos) { return getLine(startPos, endPos, Color.white); }

        public static bool returnLine(NocabLine oldLine) {
            // Return the line back into the memory pool
            freeLines.Add(oldLine);
            return true;
        }
    }

    void OnPostRender() {
        drawAllLines();
    }

    private void drawAllLines() {
        foreach (NocabLine l in lines.Values) {
            drawLine(l.startPt, l.endPt, l.color); 
        }
    }

    private void drawLine(Vector2 p1, Vector2 p2, Color c) {
        // Draw a line starting at p1 and ending at p2
        GL.Begin(GL.LINES);
        lineMat.SetPass(0);
        GL.Color(c);
        GL.Vertex(p1);
        GL.Vertex(p2);
        GL.End();
    }


    public static int AddLine(Vector2 startPt, Vector2 endPt, Color color) {
        /* A line between the given points will be drawn. 
         * The returned value represents the hash or name 
         * of the line and should be presented to the 
         * RemoveLine(...) function if the line is to be 
         * removed.
         * 
         * The returned line hash will NEVER be 0.
         */
        previousLineName += 1;
        int newLineName = previousLineName;
        NocabLine newLine = NocabLinePooler.getLine(startPt, endPt, color);
        lines.Add(newLineName, newLine);
        return previousLineName;
    }

    public static int AddLine(Vector2 startPt, Vector2 endPt) { return AddLine(startPt, endPt, Color.white); }

    public static bool RemoveLine(int lineName) {
        /* Removes the line associated with the provided lineName hash.
         * If no such has exists, or was previously deleted this function
         * return false. Otherwise, returning true means that the line
         * was sucesfully removed.
         */
        if (lines.ContainsKey(lineName)) {
            NocabLine oldLine = lines[lineName];
            NocabLinePooler.returnLine(oldLine);
            lines.Remove(lineName);
            return true;
        }

        return false;
    }

}
