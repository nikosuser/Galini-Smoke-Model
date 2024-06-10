function [y,x] = inf_lin_inv(Loc)
    
    n=1;
    primeLoc=1;
    
    while Loc>=primeLoc
        primeLoc=primeLoc+8*n;
        n=n+1;
    end

    n=n-1;
    primeLoc=primeLoc-8*n;

    X=Loc-primeLoc;
    if X<n
        y=n;
        x=X;
    elseif X < 3*n
        y=2*n-X;
        x=n;
    elseif X < 5*n
        x=4*n-X;
        y=-n;
    elseif X<7*n
        x=-n;
        y=6*n-X;
    else
        x=Loc-(primeLoc+8*n);
        y=n;
    end
end