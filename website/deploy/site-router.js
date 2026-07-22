// CloudFront Function (viewer-request) shared by both distributions.
//  1. Redirects www.benzene.app -> https://benzene.app (301). The dev distribution never sees a
//     www host, so that branch is inert there — one function serves both.
//  2. Rewrites directory-style requests to their index.html (CloudFront only applies
//     default_root_object at the root, not for sub-paths), so /docs/ -> /docs/index.html.
function handler(event) {
    var request = event.request;
    var host = request.headers.host.value;

    if (host === 'www.benzene.app') {
        return {
            statusCode: 301,
            statusDescription: 'Moved Permanently',
            headers: { location: { value: 'https://benzene.app' + request.uri } }
        };
    }

    var uri = request.uri;
    if (uri.endsWith('/')) {
        request.uri = uri + 'index.html';
    } else if (!uri.split('/').pop().includes('.')) {
        request.uri = uri + '/index.html';
    }

    return request;
}
