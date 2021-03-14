package it.unimib.turistafelice.model;

public class UnsplashResults {
    private String id;
    private UnsplashUrls urls;

    public UnsplashResults(String id, UnsplashUrls urls) {
        this.id = id;
        this.urls = urls;
    }

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public UnsplashUrls getUrls() {
        return urls;
    }

    public void setUrls(UnsplashUrls urls) {
        this.urls = urls;
    }

    @Override
    public String toString() {
        return "UnsplashResults{" +
                "id='" + id + '\'' +
                ", urls=" + urls +
                '}';
    }
}
