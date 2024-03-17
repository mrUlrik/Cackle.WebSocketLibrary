namespace vtortola.WebSockets
{
    public delegate Task<bool> HttpAuthenticationCallback(WebSocketHttpRequest request, WebSocketHttpResponse response);
}