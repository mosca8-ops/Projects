package it.unimib.turistafelice.model;

public class Trip {
    private String name;
    private String urlPhoto;

    public Trip() {
    }

    public Trip(String name, String urlPhoto) {
        this.name = name;
        this.urlPhoto = urlPhoto;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getUrlPhoto() {
        return urlPhoto;
    }

    public void setUrlPhoto(String urlPhoto) {
        this.urlPhoto = urlPhoto;
    }

    @Override
    public String toString() {
        return "Trip{" +
                "name='" + name + '\'' +
                ", urlPhoto='" + urlPhoto + '\'' +
                '}';
    }
}
