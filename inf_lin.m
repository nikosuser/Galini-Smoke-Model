function [Loc] = inf_lin(coords)

    x=coords(1);
    y=coords(2);

    if abs(x)>abs(y)
        m=abs(x);
    else
        m=abs(y);
    end
    Loc=4*m*(m-1)+1;
    
    if y==m
        if x>0
            X=x;
        else
            X=-x;
        end
    elseif y==-m
        X=3*m+abs(x-m);
    elseif x==m
        X=m+abs(y-m);
    elseif x==-m
        X=5*m+abs(m+y);
    end
    
    Loc=Loc+X;

end



