#include <bits/stdc++.h>

#ifdef LOCAL
#define Debug(x) std::cout << "$ Debug(" << #x << ") = " << x << "\n"
#else
#define Debug(x)
#endif

#define STACK_SIZE 32
namespace OI {

	bool f;
	char ch;
	
	template <class T>
	void read(T &x) {
		x = f = 0;
		ch = getchar();
		while (ch < '0' || ch > '9') {
			if (ch == '-') f = true;
			ch = getchar();
		}
		while (ch >= '0' && ch <='9')
			x = x * 10 + ch - 48,
			ch = getchar();
		if (f) x = -x;
	}
	
	int top, stack[STACK_SIZE];
	template <class T>
	void _write(T x) {
		top = 0;
		do {
			stack[top++] = x % 10, x /= 10;
		} while(x);
		while (top) putchar(stack[--top] + '0');
	}
	
	template <class T>
	void write(T x) {
		if (x < 0) x = -x, putchar('-');
		_write<T>(x);
	}
	
	template <class T>
	inline T abs(T x) { return x < 0 ? -x : x; }
	template <class T>
	inline T min(T x, T y) { return x < y ? x : y; }
	template <class T>
	inline T max(T x, T y) { return x > y ? x : y; }
}
using OI::read;
using OI::write;

#define MX 400005
#define ll long long
#define ls(x) (x << 1)
#define rs(x) (x << 1 | 1)
#define pushup(x) (a[x].v = a[ls(x)].v + a[rs(x)].v)

class Node {
public:
	ll v, t, l, r;
	Node() { v = t = l = r = 0; }
} a[MX];

ll i = 1, n, m, k, mode, x, y, init[MX];

void build(ll p, ll l, ll r) {
	a[p].l = l, a[p].r = r;
	if (l == r) {
		a[p].v = init[l];
		return;
	}
	
	ll mid = (l + r) >> 1;
	build(ls(p), l, mid);
	build(rs(p), mid + 1, r);
	pushup(p);
}

inline void pushdown(ll p) {
	if (a[p].t) {
		a[ls(p)].t += a[p].t;
		a[ls(p)].v += a[p].t * (a[ls(p)].r - a[ls(p)].l + 1);
		a[rs(p)].t += a[p].t;
		a[rs(p)].v += a[p].t * (a[rs(p)].r - a[rs(p)].l + 1);
		a[p].t = 0;
	}
}

void update(ll p, ll v, ll l, ll r) {
	if (l <= a[p].l && a[p].r <= r) {
		a[p].v += v * (a[p].r - a[p].l + 1);
		a[p].t += v;
		return;
	}
	
	pushdown(p);
	ll mid = (a[p].l + a[p].r) >> 1;
	if (l <= mid) update(ls(p), v, l, r);
	if (r > mid) update(rs(p), v, l, r);
	pushup(p);
}

ll query(ll p, ll l, ll r) {
	if (l <= a[p].l && a[p].r <= r) return a[p].v;
	
	pushdown(p);
	ll mid = (a[p].l + a[p].r) >> 1, rt = 0;
	if (l <= mid) rt += query(ls(p), l, r);
	if (r > mid) rt += query(rs(p), l, r);
	return rt;
}

int main() {
	read(n); read(m);
	for (; i <= n; ++i) read(init[i]);
	build(1, 1, n);
	while (m -- ) {
		read(mode); read(x); read(y);
		if (mode == 1) {
			read(k);
			update(1, k, x, y);
		}
		else write(query(1, x, y)), putchar('\n');
	}
	return 0;
}