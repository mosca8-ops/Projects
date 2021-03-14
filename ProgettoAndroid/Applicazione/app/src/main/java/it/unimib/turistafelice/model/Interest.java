package it.unimib.turistafelice.model;

public class Interest {

    private String name;
    private Boolean isWanted;

    public Interest(String name, Boolean isWanted) {
        this.name = name;
        this.isWanted = isWanted;
    }

    public String getName() {
        return name;
    }

    public Boolean getWanted() {
        return isWanted;
    }

    public void setWanted(Boolean wanted) {
        isWanted = wanted;
    }
}
