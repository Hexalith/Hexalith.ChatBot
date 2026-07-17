/** @type {import('@commitlint/types').UserConfig} */
module.exports = {
    rules: {
        'header-max-length': [2, 'always', 100],
        'subject-empty': [2, 'never'],
        'type-empty': [2, 'never'],
        'type-enum': [
            2,
            'always',
            [
                'build',
                'ci',
                'docs',
                'feat',
                'fix',
                'perf',
                'refactor',
                'revert',
                'style',
                'test',
            ],
        ],
    },
};
