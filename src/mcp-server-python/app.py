import os
import logging
from mcp.server.fastmcp import FastMCP
from bs4 import BeautifulSoup
import requests
from starlette.applications import Starlette
from starlette.routing import Mount
from starlette.responses import JSONResponse
import uvicorn

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Create FastMCP server (stateless for production deployment)
mcp = FastMCP("TechCrunch News Server", stateless_http=True)

@mcp.tool()
def fetch_from_techcrunch(category: str = "latest") -> str:
    """
    Fetch the latest news from TechCrunch for a given category.
    
    Args:
        category: The news category to fetch (ai, startup, security, venture, latest)
        
    Returns:
        Latest news content from TechCrunch
    """
    allowed = {"ai", "startup", "security", "venture", "latest"}
    cat = category.lower()

    if cat not in allowed:
        cat = "latest"
        logger.warning(f"Invalid category '{category}', defaulting to 'latest'")

    url = f"https://techcrunch.com/tag/{cat}/" if cat != "latest" else "https://techcrunch.com/"
    
    logger.info(f"Fetching TechCrunch news from category: {cat}")

    try:
        headers = {
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        }
        response = requests.get(url, headers=headers, timeout=10)
        
        if response.ok:
            try:
                soup = BeautifulSoup(response.text, "html.parser")
                
                # Extract article titles and summaries more intelligently
                articles = []
                for article in soup.find_all(['article', 'div'], class_=lambda x: x and ('post' in x.lower() or 'article' in x.lower()))[:5]:
                    title_elem = article.find(['h1', 'h2', 'h3'], class_=lambda x: x and 'title' in x.lower())
                    if title_elem:
                        title = title_elem.get_text(strip=True)
                        if title and len(title) > 10:  # Filter out very short titles
                            articles.append(title)
                
                if articles:
                    result = f"Latest TechCrunch {cat} news:\n\n" + "\n".join([f"• {article}" for article in articles])
                    return result[:2000] + ("..." if len(result) > 2000 else "")
                else:
                    # Fallback to general text extraction
                    text = soup.get_text(separator=' ', strip=True)
                    return f"TechCrunch {cat} news content:\n" + text[:1000] + ("..." if len(text) > 1000 else "")
                    
            except ImportError:
                return f"TechCrunch {cat} content:\n" + response.text[:1000] + ("..." if len(response.text) > 1000 else "")
        else:
            return f"Failed to fetch news from TechCrunch. HTTP Status: {response.status_code}"
            
    except requests.exceptions.Timeout:
        return "Error: Request timeout while fetching TechCrunch news"
    except requests.exceptions.RequestException as e:
        return f"Error fetching TechCrunch news: {str(e)}"
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
        return f"Unexpected error while fetching news: {str(e)}"

# Create ASGI application for production deployment
async def root_handler(request):
    """Health check endpoint"""
    return JSONResponse({
        "service": "TechCrunch News MCP Server",
        "status": "healthy",
        "transport": "streamable-http",
        "mcp_endpoint": "/mcp"
    })

# Create Starlette app with MCP server mounted at /mcp
app = Starlette(
    routes=[
        Mount("/mcp", mcp.streamable_http_app()),
    ]
)

# Add root health check
@app.route("/")
async def health_check(request):
    return await root_handler(request)

if __name__ == "__main__":
    logger.info("Starting TechCrunch News MCP server with streamable HTTP transport")
    logger.info("🚀 TechCrunch MCP Server starting on port 8000")
    logger.info("📍 MCP endpoint available at /mcp")
    logger.info("🏥 Health check available at /")
    
    # Run with uvicorn for production
    uvicorn.run(app, host="0.0.0.0", port=8000, log_level="info")