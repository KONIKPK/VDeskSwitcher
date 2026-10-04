namespace VDeskSwitcher;

internal enum EdgeDirection { None = 0, Previous = -1, Next = 1 }

internal sealed class EdgeDwell
{
    private long? enteredAt;
    private EdgeDirection pending;
    private EdgeDirection latched;

    internal EdgeDirection Update(long milliseconds, int cursorX, int screenLeft, int screenWidth, bool suppressed)
    {
        if (screenWidth <= 0)
        {
            CancelPending();
            return EdgeDirection.None;
        }
        long right = (long)screenLeft + screenWidth - 1;
        if (latched != EdgeDirection.None)
        {
            bool movedAway = latched == EdgeDirection.Previous
                ? (long)cursorX - screenLeft >= EdgeSettings.RearmDistancePx
                : right - cursorX >= EdgeSettings.RearmDistancePx;
            if (!movedAway) return EdgeDirection.None;
            latched = EdgeDirection.None;
        }
        EdgeDirection edge = cursorX >= screenLeft && cursorX < (long)screenLeft + EdgeSettings.EdgeZonePx
            ? EdgeDirection.Previous
            : cursorX <= right && cursorX > right - EdgeSettings.EdgeZonePx ? EdgeDirection.Next : EdgeDirection.None;
        if (suppressed || edge == EdgeDirection.None)
        {
            CancelPending();
            return EdgeDirection.None;
        }
        if (edge != pending) { pending = edge; enteredAt = milliseconds; }
        enteredAt ??= milliseconds;
        if (milliseconds - enteredAt.Value < EdgeSettings.DwellMilliseconds) return EdgeDirection.None;
        latched = edge;
        CancelPending();
        return edge;
    }

    internal void CancelPending()
    {
        enteredAt = null;
        pending = EdgeDirection.None;
    }
}
