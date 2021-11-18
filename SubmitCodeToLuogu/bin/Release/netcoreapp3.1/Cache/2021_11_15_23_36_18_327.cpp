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

#define MX 10005

int i, n, k, m, f[MX];
void print() 
{
	write(m), putchar('\n');
	for (i = 1; i <= m; ++i)
		write(f[i]), putchar(' ');
}
int main() {
	read(n); read(k);
	for (f[0] = -114514, i = 2; i <= n; ++i)
	{
		while (n % i == 0 && k >= i) {
			n /= i, k -= i;
			f[++m] = i;
			//if (f[m] == f[m - 1]) f[--m] *= i;
		}
		if (m > 1000) return write(-1), 0;
		if (k < i) break;
		if (n == 1) break;
	}
//	Debug(n); Debug(k);
	if (n == 1)
	{
		while (k) {
			f[++m] = (--k, 1);
			if (m > 1000) return write(-1), 0;
		}
		return print(), 0;
	}
	return write(-1), 0;
}